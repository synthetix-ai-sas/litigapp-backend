using FluentAssertions;
using LitigApp.Application.Common.Abstractions;
using LitigApp.Domain.Processes;
using LitigApp.Infrastructure.Persistence;
using LitigApp.Infrastructure.Persistence.Repositories;
using LitigApp.Infrastructure.Sync;
using LitigApp.Jobs;
using LitigApp.Jobs.ProcessSyncJobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;

namespace LitigApp.Jobs.IntegrationTests;

public sealed class ActionsSweepJobTests : IAsyncLifetime
{
    private static readonly DateTime FixedNow = new(2026, 6, 22, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Day = new(2026, 3, 20);

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();
    private AppDbContext _db = null!;
    private readonly FakeClock _clock = new() { UtcNow = FixedNow };

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options;
        _db = new AppDbContext(options);
        await _db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task NewActions_AreInserted_Grouped_AndUserNotified()
    {
        var process = PendingProcess("user-a", externalId: 999, lastConsecutive: 80);
        _db.Processes.Add(process);
        await _db.SaveChangesAsync(CancellationToken.None);

        var scheduler = new RecordingSyncJobScheduler();
        var client = new FakeActionsClient(RamaResult<List<ActionData>>.Ok(
        [
            Act(5001, 81, "Auto admite demanda"),
            Act(5002, 82, "Fijacion estado"),
        ]));

        await BuildJob(client, scheduler).RunAsync();

        var actions = await _db.ProcessActions.AsNoTracking()
            .Where(a => a.ProcessId == process.Id).OrderBy(a => a.ConsecutiveNumber).ToListAsync(CancellationToken.None);
        actions.Should().HaveCount(2);

        var fijacion = actions.Single(a => a.ConsecutiveNumber == 82);
        var auto = actions.Single(a => a.ConsecutiveNumber == 81);
        fijacion.GroupedWithId.Should().Be(auto.Id); // Auto+Fijación grouping persisted

        var updated = await _db.Processes.AsNoTracking().FirstAsync(p => p.Id == process.Id, CancellationToken.None);
        updated.Attended.Should().BeFalse();
        updated.SyncPhase.Should().Be("idle");
        updated.SyncStatus.Should().Be(ProcessSyncStatus.Ok);
        updated.LastExternalConsecutive.Should().Be(82);
        updated.CurrentStatus.Should().Be("Fijacion estado");

        scheduler.NotifiedUserIds.Should().ContainSingle().Which.Should().Be("user-a");
    }

    [Fact]
    public async Task RunningTwice_IsIdempotent_NoDuplicateActionsOrNotifications()
    {
        var process = PendingProcess("user-b", externalId: 999, lastConsecutive: 80);
        _db.Processes.Add(process);
        await _db.SaveChangesAsync(CancellationToken.None);

        var scheduler = new RecordingSyncJobScheduler();
        var client = new FakeActionsClient(RamaResult<List<ActionData>>.Ok(
        [
            Act(5001, 81, "Auto admite demanda"),
            Act(5002, 82, "Fijacion estado"),
        ]));

        // First run: inserts both actions, notifies the user once.
        await BuildJob(client, scheduler).RunAsync();

        // Simulate a re-trigger of the same process with the same API page (e.g. crashed/re-queued).
        var tracked = await _db.Processes.FirstAsync(p => p.Id == process.Id, CancellationToken.None);
        tracked.SyncPhase = "pending_actions";
        await _db.SaveChangesAsync(CancellationToken.None);

        // Second run: nothing new should be inserted, no second notification.
        await BuildJob(client, scheduler).RunAsync();

        var count = await _db.ProcessActions.AsNoTracking()
            .CountAsync(a => a.ProcessId == process.Id, CancellationToken.None);
        count.Should().Be(2);

        var updated = await _db.Processes.AsNoTracking().FirstAsync(p => p.Id == process.Id, CancellationToken.None);
        updated.LastExternalConsecutive.Should().Be(82);
        scheduler.NotifiedUserIds.Should().ContainSingle(); // only the first run notified
    }

    [Fact]
    public async Task NoNewActions_DoesNotNotifyOrChangeAttended()
    {
        var process = PendingProcess("user-c", externalId: 999, lastConsecutive: 82);
        process.Attended = true;
        _db.Processes.Add(process);
        _db.ProcessActions.Add(ExistingAction(process.Id, 6001, 82, "Fijacion estado"));
        await _db.SaveChangesAsync(CancellationToken.None);

        var scheduler = new RecordingSyncJobScheduler();
        var client = new FakeActionsClient(RamaResult<List<ActionData>>.Ok([Act(6001, 82, "Fijacion estado")]));

        await BuildJob(client, scheduler).RunAsync();

        var count = await _db.ProcessActions.AsNoTracking()
            .CountAsync(a => a.ProcessId == process.Id, CancellationToken.None);
        count.Should().Be(1);

        var updated = await _db.Processes.AsNoTracking().FirstAsync(p => p.Id == process.Id, CancellationToken.None);
        updated.Attended.Should().BeTrue();
        updated.SyncPhase.Should().Be("idle");
        scheduler.NotifiedUserIds.Should().BeEmpty();
    }

    [Fact]
    public async Task WafBlocked_SetsCooldown_Reschedules_AndLeavesProcessPending()
    {
        var process = PendingProcess("user-d", externalId: 999, lastConsecutive: 80);
        _db.Processes.Add(process);
        await _db.SaveChangesAsync(CancellationToken.None);

        var scheduler = new RecordingSyncJobScheduler();
        var client = new FakeActionsClient(RamaResult<List<ActionData>>.Fail(FailureKind.WafBlocked, "WAF 403"));

        await BuildJob(client, scheduler).RunAsync();

        var blockedUntil = await new SyncStateService(_db, _clock).GetWafBlockedUntilAsync(CancellationToken.None);
        blockedUntil.Should().BeCloseTo(
            new DateTimeOffset(FixedNow, TimeSpan.Zero).AddMinutes(20), TimeSpan.FromSeconds(2));

        scheduler.ActionsSweepScheduled.Should().Be(1);

        var updated = await _db.Processes.AsNoTracking().FirstAsync(p => p.Id == process.Id, CancellationToken.None);
        updated.SyncPhase.Should().Be("pending_actions"); // untouched, retried after cooldown
    }

    [Fact]
    public async Task WhenCooldownActive_ReschedulesWithoutCallingApi()
    {
        await new SyncStateService(_db, _clock).SetWafBlockedUntilAsync(
            new DateTimeOffset(FixedNow, TimeSpan.Zero).AddMinutes(20), "prior 403", CancellationToken.None);

        var process = PendingProcess("user-e", externalId: 999, lastConsecutive: 80);
        _db.Processes.Add(process);
        await _db.SaveChangesAsync(CancellationToken.None);

        var scheduler = new RecordingSyncJobScheduler();
        var client = new FakeActionsClient(RamaResult<List<ActionData>>.Ok([]));

        await BuildJob(client, scheduler).RunAsync();

        client.ActionsCallCount.Should().Be(0);
        scheduler.ActionsSweepScheduled.Should().Be(1);

        var untouched = await _db.Processes.AsNoTracking().FirstAsync(p => p.Id == process.Id, CancellationToken.None);
        untouched.LastSyncAttemptAt.Should().BeNull();
    }

    // ── helpers ────────────────────────────────────────────────────────────────

    private ActionsSweepJob BuildJob(IRamaJudicialClient client, ISyncJobScheduler scheduler) => new(
        new ProcessRepository(_db),
        client,
        new SyncStateService(_db, _clock),
        scheduler,
        new NoOpSyncDelay(),
        Options.Create(new SweepOptions { BatchSize = 100, MinimumHoursBetweenSyncsPerProcess = 22 }),
        Options.Create(new ThrottleOptions()),
        Options.Create(new WafOptions()),
        _clock,
        NullLogger<ActionsSweepJob>.Instance);

    private static Process PendingProcess(string userId, long externalId, int lastConsecutive) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        FileNumber = "11001310300120230001200",
        ExternalProcessId = externalId,
        SyncPhase = "pending_actions",
        SyncStatus = ProcessSyncStatus.Ok,
        IsActive = true,
        Attended = true,
        LastExternalConsecutive = lastConsecutive,
        CreatedAt = DateTimeOffset.UtcNow.AddDays(-10),
        UpdatedAt = DateTimeOffset.UtcNow.AddDays(-10),
    };

    private static ProcessAction ExistingAction(Guid processId, long externalId, int consecutive, string type) => new()
    {
        Id = Guid.NewGuid(),
        ProcessId = processId,
        ExternalActionId = externalId,
        ConsecutiveNumber = consecutive,
        Action = type,
        RecordedAt = Day,
        CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
    };

    private static ActionData Act(long id, int consecutive, string type)
    {
        var recorded = Day.ToDateTime(TimeOnly.MinValue);
        return new ActionData(id, consecutive, recorded, type, null, null, null, recorded, null, false);
    }

    private sealed class FakeClock : IDateTimeProvider
    {
        public DateTime UtcNow { get; set; }
    }

    private sealed class FakeActionsClient(RamaResult<List<ActionData>> result) : IRamaJudicialClient
    {
        public int ActionsCallCount { get; private set; }

        public Task<RamaResult<List<ActionData>>> GetFirstPageActionsAsync(long externalProcessId, CancellationToken ct)
        {
            ActionsCallCount++;
            return Task.FromResult(result);
        }

        public Task<RamaResult<OverviewData?>> GetOverviewByFileNumberAsync(string fileNumber, CancellationToken ct) =>
            throw new NotSupportedException();

        public Task<RamaResult<ProcessDetailData?>> GetDetailAsync(long externalProcessId, CancellationToken ct) =>
            throw new NotSupportedException();

        public Task<RamaResult<List<SubjectData>>> GetSubjectsAsync(long externalProcessId, CancellationToken ct) =>
            throw new NotSupportedException();
    }
}
