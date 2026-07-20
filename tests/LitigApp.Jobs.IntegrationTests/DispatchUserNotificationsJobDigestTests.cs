using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Notifications;
using LitigApp.Domain.Common;
using LitigApp.Domain.Processes;
using LitigApp.Infrastructure.Imports;
using LitigApp.Infrastructure.Notifications.Templates;
using LitigApp.Infrastructure.Persistence;
using LitigApp.Infrastructure.Persistence.Repositories;
using LitigApp.Infrastructure.Time;
using LitigApp.Jobs.ProcessSyncJobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;

namespace LitigApp.Jobs.IntegrationTests;

/// <summary>
/// End-to-end digest acceptance tests (blueprint §10.3): real Postgres + real Scriban
/// renderer, but a RECORDING fake for IEmailSender — NEVER hits real Resend. Verifies the
/// core rule ("1 email per user, not N") and the idempotency watermark (notification_logs).
/// </summary>
public sealed class DispatchUserNotificationsJobDigestTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();
    private AppDbContext _db = null!;
    private readonly SystemDateTimeProvider _clock = new();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options;
        _db = new AppDbContext(opts);
        await _db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task ThreeChangedProcesses_ResultInExactlyOneEmail_NotThree()
    {
        const string userId = "user-digest-1";
        await SeedChangedProcessesAsync(userId, count: 3);

        var emailSender = new RecordingEmailSender();
        var job = BuildJob(emailSender, userId, "sergio@example.com", "Sergio Molina");

        await job.RunAsync(userId);

        Assert.Equal(1, emailSender.SentCount);
        Assert.Contains("3 novedades", emailSender.LastSubject);
        Assert.Contains("sergio@example.com", emailSender.LastToEmail);
        Assert.DoesNotContain("{{", emailSender.LastHtmlBody); // fully rendered, no leftover placeholders

        var outbox = await _db.OutboxMessages.AsNoTracking().Where(o => o.UserId == userId).ToListAsync();
        var row = Assert.Single(outbox);
        Assert.Equal("sent", row.Status);

        var logs = await _db.NotificationLogs.AsNoTracking().Where(l => l.UserId == userId).ToListAsync();
        var log = Assert.Single(logs);
        Assert.Equal(3, log.ProcessIds.Length);
        Assert.Equal("delivered", log.Status);
    }

    [Fact]
    public async Task ReRunAfterSuccess_DoesNotSendDuplicate_IdempotentViaWatermark()
    {
        const string userId = "user-digest-2";
        await SeedChangedProcessesAsync(userId, count: 2);

        var emailSender = new RecordingEmailSender();
        var job = BuildJob(emailSender, userId, "sergio@example.com", "Sergio Molina");

        await job.RunAsync(userId);
        Assert.Equal(1, emailSender.SentCount);

        // Re-run immediately — no NEW changes since the watermark (notification_logs.SentAt)
        // advanced past every seeded process's LastCourtActionAt.
        await job.RunAsync(userId);

        Assert.Equal(1, emailSender.SentCount);
        var outbox = await _db.OutboxMessages.AsNoTracking().Where(o => o.UserId == userId).ToListAsync();
        Assert.Single(outbox);
        var logs = await _db.NotificationLogs.AsNoTracking().Where(l => l.UserId == userId).ToListAsync();
        Assert.Single(logs);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private async Task SeedChangedProcessesAsync(string userId, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var processId = Guid.NewGuid();
            _db.Processes.Add(new Process
            {
                Id = processId,
                UserId = userId,
                FileNumber = $"17001400301020240{i:000000}", // 17 + 6 digits = 23
                IsActive = true,
                SyncStatus = "ok",
                SyncPhase = "idle",
                LastCourtActionAt = DateTimeOffset.UtcNow.AddHours(-1 - i),
                Attended = false,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            });
            _db.ProcessActions.Add(new ProcessAction
            {
                Id = Guid.NewGuid(),
                ProcessId = processId,
                ExternalActionId = 1000 + i,
                ConsecutiveNumber = 1,
                Action = "Fijacion estado",
                Annotation = "Actuacion registrada",
                ActionDate = DateOnly.FromDateTime(DateTime.UtcNow),
                CreatedAt = DateTimeOffset.UtcNow,
            });
        }

        await _db.SaveChangesAsync();
    }

    private DispatchUserNotificationsJob BuildJob(
        RecordingEmailSender emailSender, string userId, string email, string fullName)
    {
        var processRepo = new ProcessRepository(_db);
        var logRepo = new NotificationLogRepository(_db);
        var outboxRepo = new OutboxRepository(_db);
        var renderer = new ScribanEmailTemplateRenderer();
        var identity = new FakeIdentityService(userId, email, fullName);
        var notificationsOptions = Options.Create(new NotificationsOptions
        {
            DigestMaxRows = 5,
            AppBaseUrl = "https://app.litigapp.co",
        });

        var dispatchService = new NotificationDispatchService(
            identity, renderer, emailSender, outboxRepo, logRepo, new ImportErrorsCsvBuilder(),
            notificationsOptions, _clock);

        return new DispatchUserNotificationsJob(
            processRepo, logRepo, outboxRepo, dispatchService, _clock,
            NullLogger<DispatchUserNotificationsJob>.Instance);
    }

    private sealed class RecordingEmailSender : IEmailSender
    {
        public int SentCount { get; private set; }
        public string? LastToEmail { get; private set; }
        public string? LastSubject { get; private set; }
        public string? LastHtmlBody { get; private set; }

        public Task<Result<string>> SendAsync(
            string toEmail, string subject, string htmlBody, string? idempotencyKey = null,
            IReadOnlyList<EmailAttachment>? attachments = null, CancellationToken ct = default)
        {
            SentCount++;
            LastToEmail = toEmail;
            LastSubject = subject;
            LastHtmlBody = htmlBody;
            return Task.FromResult(Result<string>.Success($"fake-{SentCount}"));
        }
    }

    private sealed class FakeIdentityService(string expectedUserId, string email, string fullName) : IIdentityService
    {
        public Task<UserProfile?> GetUserProfileAsync(string userId, CancellationToken ct = default) =>
            Task.FromResult(userId == expectedUserId ? new UserProfile(email, fullName) : null);

        public Task<bool> IsEmailRegisteredAsync(string email, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<IdentityOperationResult> CreateUserAsync(
            string email, string password, string fullName, string? whatsAppPhone, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<string?> ValidateCredentialsAsync(string email, string password, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<string>> GetUserRolesAsync(string userId, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<string?> GetUserEmailAsync(string userId, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<string?> GetPasswordResetTokenAsync(string email, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<IdentityOperationResult> ResetPasswordAsync(
            string email, string resetToken, string newPassword, CancellationToken ct = default) =>
            throw new NotImplementedException();
    }
}
