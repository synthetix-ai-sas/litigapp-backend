using FluentAssertions;
using Hangfire;
using Hangfire.PostgreSql;
using LitigApp.Application.Common.Abstractions;
using LitigApp.Domain.Processes;
using LitigApp.Infrastructure.Persistence;
using LitigApp.Infrastructure.Persistence.Repositories;
using LitigApp.Infrastructure.Sync;
using LitigApp.Infrastructure.Time;
using LitigApp.Jobs;
using LitigApp.Jobs.ProcessSyncJobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;

namespace LitigApp.Jobs.IntegrationTests;

public sealed class OverviewSweepJobTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    private AppDbContext _db = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options;

        _db = new AppDbContext(options);
        await _db.Database.MigrateAsync();

        // Configure Hangfire storage against same Postgres (needed for RecurringJob.AddOrUpdate)
        GlobalConfiguration.Configuration
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(
                opts => opts.UseNpgsqlConnection(_postgres.GetConnectionString()),
                new PostgreSqlStorageOptions { SchemaName = "hangfire", PrepareSchemaIfNecessary = true });
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public void RegisterRecurringJobs_Registers_OverviewSweep()
    {
        // InitializeAsync configured GlobalConfiguration with the Testcontainers Postgres,
        // so new RecurringJobManager() picks up the correct storage.
        var manager = new RecurringJobManager();
        var sweepOpts = new SweepOptions
        {
            OverviewIntervalMinutes = 15,
            BatchSize = 50,
            MinimumHoursBetweenSyncsPerProcess = 22,
        };

        var act = () => HangfireConfiguration.RegisterRecurringJobs(manager, sweepOpts);
        act.Should().NotThrow();
    }

    [Fact]
    public async Task RunAsync_WhenProcessHasNewAction_MarksAsPendingActions()
    {
        // Arrange — process that was last synced > 22h ago with an old LastCourtActionAt
        var oldActionDate = DateTimeOffset.UtcNow.AddDays(-2);
        var newActionDate = oldActionDate.AddDays(1); // newer than stored

        var process = new Process
        {
            Id = Guid.NewGuid(),
            UserId = "user-1",
            FileNumber = "11001310300120230001200",
            SyncPhase = "idle",
            SyncStatus = "ok",
            IsActive = true,
            LastCourtActionAt = oldActionDate,
            LastSyncedAt = DateTimeOffset.UtcNow.AddHours(-25), // older than 22h threshold
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-10),
            UpdatedAt = DateTimeOffset.UtcNow.AddDays(-10),
        };
        _db.Processes.Add(process);
        await _db.SaveChangesAsync();

        var fakeClient = new FakeRamaJudicialClient(
            overviewResult: RamaResult<OverviewData?>.Ok(
                new OverviewData(
                    ExternalProcessId: 999L,
                    ExternalConnectionId: 1,
                    ProcessKey: "11001310300120230001200",
                    LastActionDate: newActionDate.DateTime,
                    CourtName: "Juzgado de prueba",
                    Department: "Bogotá D.C.",
                    IsPrivate: false)));

        var job = BuildJob(fakeClient);

        // Act
        await job.RunAsync();

        // Assert
        var updated = await _db.Processes.AsNoTracking()
            .FirstAsync(p => p.Id == process.Id);

        updated.SyncPhase.Should().Be("pending_actions");
        updated.LastSyncAttemptAt.Should().NotBeNull();
        updated.ExternalProcessId.Should().Be(999L);
    }

    [Fact]
    public async Task RunAsync_WhenNoChanges_MarksAsIdle()
    {
        // Arrange — process with same LastCourtActionAt as what the API returns
        var actionDate = DateTimeOffset.UtcNow.AddDays(-3);

        var process = new Process
        {
            Id = Guid.NewGuid(),
            UserId = "user-2",
            FileNumber = "11001310300120230001201",
            SyncPhase = "idle",
            SyncStatus = "ok",
            IsActive = true,
            LastCourtActionAt = actionDate,
            LastSyncedAt = DateTimeOffset.UtcNow.AddHours(-25),
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-5),
            UpdatedAt = DateTimeOffset.UtcNow.AddDays(-5),
        };
        _db.Processes.Add(process);
        await _db.SaveChangesAsync();

        var fakeClient = new FakeRamaJudicialClient(
            overviewResult: RamaResult<OverviewData?>.Ok(
                new OverviewData(
                    ExternalProcessId: 888L,
                    ExternalConnectionId: 1,
                    ProcessKey: "11001310300120230001201",
                    LastActionDate: actionDate.DateTime, // same date — no change
                    CourtName: "Juzgado de prueba",
                    Department: "Bogotá D.C.",
                    IsPrivate: false)));

        var job = BuildJob(fakeClient);

        // Act
        await job.RunAsync();

        // Assert
        var updated = await _db.Processes.AsNoTracking()
            .FirstAsync(p => p.Id == process.Id);

        updated.SyncPhase.Should().Be("idle");
        updated.SyncStatus.Should().Be("ok");
        updated.LastSyncedAt.Should().NotBeNull();
        updated.LastSyncedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task RunAsync_WhenNotFound_MarksAsNotFound()
    {
        var process = new Process
        {
            Id = Guid.NewGuid(),
            UserId = "user-3",
            FileNumber = "11001310300120230001202",
            SyncPhase = "pending_overview",
            SyncStatus = "pending",
            IsActive = true,
            LastSyncedAt = DateTimeOffset.UtcNow.AddHours(-25),
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
            UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1),
        };
        _db.Processes.Add(process);
        await _db.SaveChangesAsync();

        var fakeClient = new FakeRamaJudicialClient(
            overviewResult: RamaResult<OverviewData?>.Fail(FailureKind.NotFound, "Not found"));

        var job = BuildJob(fakeClient);

        // Act
        await job.RunAsync();

        // Assert
        var updated = await _db.Processes.AsNoTracking()
            .FirstAsync(p => p.Id == process.Id);

        updated.SyncPhase.Should().Be("idle");
        updated.SyncStatus.Should().Be("not_found");
    }

    // ── helpers ────────────────────────────────────────────────────────────────

    private OverviewSweepJob BuildJob(IRamaJudicialClient client)
    {
        var repo = new ProcessRepository(_db);

        var opts = Options.Create(new SweepOptions
        {
            OverviewIntervalMinutes = 15,
            BatchSize = 100,
            MinimumHoursBetweenSyncsPerProcess = 22,
        });

        var clock = new SystemDateTimeProvider();

        return new OverviewSweepJob(
            repo,
            client,
            new SyncStateService(_db, clock),
            new RecordingSyncJobScheduler(),
            opts,
            Options.Create(new WafOptions()),
            clock,
            NullLogger<OverviewSweepJob>.Instance);
    }

    // ── fake client ────────────────────────────────────────────────────────────

    private sealed class FakeRamaJudicialClient(
        RamaResult<OverviewData?> overviewResult) : IRamaJudicialClient
    {
        public Task<RamaResult<OverviewData?>> GetOverviewByFileNumberAsync(
            string fileNumber, CancellationToken ct) =>
            Task.FromResult(overviewResult);

        public Task<RamaResult<ProcessDetailData?>> GetDetailAsync(
            long externalProcessId, CancellationToken ct) =>
            throw new NotSupportedException("Not needed in 2.C");

        public Task<RamaResult<List<SubjectData>>> GetSubjectsAsync(
            long externalProcessId, CancellationToken ct) =>
            throw new NotSupportedException("Not needed in 2.C");

        public Task<RamaResult<List<ActionData>>> GetFirstPageActionsAsync(
            long externalProcessId, CancellationToken ct) =>
            throw new NotSupportedException("Not needed in 2.C");
    }
}
