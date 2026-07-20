using System.Text.Json;
using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Common.Options;
using LitigApp.Application.Features.Notifications;
using LitigApp.Application.Features.Notifications.Dtos;
using LitigApp.Domain.Common;
using LitigApp.Domain.Notifications;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace LitigApp.Application.UnitTests.Features.Notifications;

public class NotificationDispatchServiceTests
{
    private readonly IIdentityService _identity = Substitute.For<IIdentityService>();
    private readonly IEmailTemplateRenderer _renderer = Substitute.For<IEmailTemplateRenderer>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly IOutboxRepository _outboxRepo = Substitute.For<IOutboxRepository>();
    private readonly INotificationLogRepository _logRepo = Substitute.For<INotificationLogRepository>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    public NotificationDispatchServiceTests()
    {
        _clock.UtcNow.Returns(new DateTime(2026, 6, 12, 0, 0, 0, DateTimeKind.Utc));
        _renderer.Render(Arg.Any<EmailTemplate>(), Arg.Any<IReadOnlyDictionary<string, object?>>())
            .Returns("<html>rendered</html>");
    }

    private NotificationDispatchService CreateSut(int digestMaxRows = 5) => new(
        _identity, _renderer, _emailSender, _outboxRepo, _logRepo,
        Options.Create(new NotificationsOptions { DigestMaxRows = digestMaxRows }),
        Options.Create(new AppOptions { FrontendBaseUrl = "https://app.litigapp.co" }),
        _clock);

    private static OutboxMessage UserDigestMessage(Guid id, string userId, params UserDigestProcessPayload[] processes)
    {
        var payload = new UserDigestOutboxPayload(processes, processes.Length, DateTimeOffset.UtcNow);
        return new OutboxMessage
        {
            Id = id, UserId = userId, EventType = "UserProcessesUpdated", Channel = "email",
            Payload = JsonSerializer.Serialize(payload), Status = "pending", CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    private static UserDigestProcessPayload Proc(int i) => new(
        Guid.NewGuid(), $"1700140030102024000000{i}", "Fijacion estado", "nota", DateTimeOffset.UtcNow.AddDays(-i));

    [Fact]
    public async Task UserProcessesUpdated_Success_SendsRendersMarksSent_InsertsLog()
    {
        var outboxId = Guid.NewGuid();
        var message = UserDigestMessage(outboxId, "user-1", Proc(1), Proc(2));
        _identity.GetUserProfileAsync("user-1", Arg.Any<CancellationToken>())
            .Returns(new UserProfile("sergio@example.com", "Sergio Molina"));
        _emailSender.SendAsync(
                "sergio@example.com", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result<string>.Success("resend-id-1"));

        await CreateSut().DispatchAsync(message, default);

        Assert.Equal("sent", message.Status);
        Assert.NotNull(message.ProcessedAt);
        await _outboxRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _logRepo.Received(1).InsertAsync(
            Arg.Is<NotificationLog>(l =>
                l.OutboxId == outboxId && l.UserId == "user-1" && l.EventType == "UserProcessesUpdated" &&
                l.Channel == "email" && l.Status == "delivered" && l.ProviderMessageId == "resend-id-1" &&
                l.ProcessIds.Length == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UserProcessesUpdated_UsesOutboxIdAsIdempotencyKey()
    {
        var outboxId = Guid.NewGuid();
        var message = UserDigestMessage(outboxId, "user-1", Proc(1));
        _identity.GetUserProfileAsync("user-1", Arg.Any<CancellationToken>())
            .Returns(new UserProfile("sergio@example.com", "Sergio Molina"));
        _emailSender.SendAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result<string>.Success("id"));

        await CreateSut().DispatchAsync(message, default);

        await _emailSender.Received(1).SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), outboxId.ToString(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UserProcessesUpdated_MoreThanMaxRows_CutsAndReportsRemaining()
    {
        var message = UserDigestMessage(Guid.NewGuid(), "user-1", Proc(1), Proc(2), Proc(3), Proc(4), Proc(5), Proc(6), Proc(7));
        _identity.GetUserProfileAsync("user-1", Arg.Any<CancellationToken>())
            .Returns(new UserProfile("sergio@example.com", "Sergio Molina"));
        _emailSender.SendAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result<string>.Success("id"));

        await CreateSut(digestMaxRows: 5).DispatchAsync(message, default);

        _renderer.Received(1).Render(EmailTemplate.UserDigest, Arg.Is<IReadOnlyDictionary<string, object?>>(m =>
            ((IEnumerable<Dictionary<string, object?>>)m["processes"]!).Count() == 5 &&
            (int)m["remaining"]! == 2));
    }

    [Fact]
    public async Task ImportComplete_Success_RendersAndSends_EmptyProcessIds()
    {
        var payload = new ImportCompleteOutboxPayload(Guid.NewGuid(), "portafolio.xlsx", 10, 1, 2, 7, DateTimeOffset.UtcNow);
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(), UserId = "user-1", EventType = "ImportComplete", Channel = "email",
            Payload = JsonSerializer.Serialize(payload), Status = "pending", CreatedAt = DateTimeOffset.UtcNow,
        };
        _identity.GetUserProfileAsync("user-1", Arg.Any<CancellationToken>())
            .Returns(new UserProfile("sergio@example.com", "Sergio Molina"));
        _emailSender.SendAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result<string>.Success("id"));

        await CreateSut().DispatchAsync(message, default);

        Assert.Equal("sent", message.Status);
        _renderer.Received(1).Render(EmailTemplate.ImportComplete, Arg.Any<IReadOnlyDictionary<string, object?>>());
        await _logRepo.Received(1).InsertAsync(
            Arg.Is<NotificationLog>(l => l.ProcessIds.Length == 0 && l.EventType == "ImportComplete"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendFails_LeavesOutboxPending_IncrementsAttempts_NoLogInserted()
    {
        var message = UserDigestMessage(Guid.NewGuid(), "user-1", Proc(1));
        message.Attempts = 2;
        _identity.GetUserProfileAsync("user-1", Arg.Any<CancellationToken>())
            .Returns(new UserProfile("sergio@example.com", "Sergio Molina"));
        _emailSender.SendAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result<string>.Failure("resend_down"));

        await CreateSut().DispatchAsync(message, default);

        Assert.Equal("pending", message.Status);
        Assert.Equal(3, message.Attempts);
        Assert.Equal("resend_down", message.LastError);
        Assert.Null(message.ProcessedAt);
        await _outboxRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _logRepo.DidNotReceive().InsertAsync(Arg.Any<NotificationLog>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UserNotFound_MarksOutboxFailed_NeverSends()
    {
        var message = UserDigestMessage(Guid.NewGuid(), "ghost-user", Proc(1));
        _identity.GetUserProfileAsync("ghost-user", Arg.Any<CancellationToken>()).Returns((UserProfile?)null);

        await CreateSut().DispatchAsync(message, default);

        Assert.Equal("failed", message.Status);
        await _emailSender.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
        await _logRepo.DidNotReceive().InsertAsync(Arg.Any<NotificationLog>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnknownEventType_MarksFailed_NeverSends()
    {
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(), UserId = "user-1", EventType = "SomethingElse", Channel = "email",
            Payload = "{}", Status = "pending", CreatedAt = DateTimeOffset.UtcNow,
        };
        _identity.GetUserProfileAsync("user-1", Arg.Any<CancellationToken>())
            .Returns(new UserProfile("sergio@example.com", "Sergio Molina"));

        await CreateSut().DispatchAsync(message, default);

        Assert.Equal("failed", message.Status);
        await _emailSender.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }
}
