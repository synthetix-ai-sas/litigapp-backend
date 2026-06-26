using LitigApp.Application.Common.Abstractions;
using LitigApp.Domain.Processes;
using LitigApp.Jobs;
using LitigApp.Jobs.ProcessSyncJobs;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace LitigApp.Jobs.UnitTests;

/// <summary>
/// Fast, DB-free tests of ActionsSweep orchestration branches: cooldown reschedule,
/// idempotent diff (dedupe by external id), notify-on-change, and 403 handling.
/// </summary>
public class ActionsSweepJobUnitTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTime Recorded = new(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc);

    private readonly IProcessRepository _repo = Substitute.For<IProcessRepository>();
    private readonly IRamaJudicialClient _client = Substitute.For<IRamaJudicialClient>();
    private readonly ISyncStateService _syncState = Substitute.For<ISyncStateService>();
    private readonly ISyncJobScheduler _scheduler = Substitute.For<ISyncJobScheduler>();
    private readonly ISyncDelay _delay = Substitute.For<ISyncDelay>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    public ActionsSweepJobUnitTests() => _clock.UtcNow.Returns(Now.UtcDateTime);

    [Fact]
    public async Task WhenCooldownActive_Reschedules_WithoutCallingApi()
    {
        _syncState.GetWafBlockedUntilAsync(Arg.Any<CancellationToken>()).Returns(Now.AddMinutes(10));

        await BuildJob().RunAsync();

        _scheduler.Received(1).ScheduleActionsSweep(Arg.Any<TimeSpan>());
        await _repo.DidNotReceive().GetPendingActionsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _client.DidNotReceive().GetFirstPageActionsAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WhenNewActions_AddsThem_MarksUnattended_AndNotifiesUser()
    {
        var process = Pending("user-a", externalId: 999);
        StubPending(process);
        StubExisting(process.Id); // none yet
        _client.GetFirstPageActionsAsync(999, Arg.Any<CancellationToken>())
            .Returns(RamaResult<List<ActionData>>.Ok([Action(5001, 81, "Auto admite")]));

        await BuildJob().RunAsync();

        await _repo.Received(1).AddActionsAsync(
            Arg.Is<IEnumerable<ProcessAction>>(a => a.Count() == 1), Arg.Any<CancellationToken>());
        Assert.False(process.Attended);
        Assert.Equal(ProcessSyncPhase.Idle, process.SyncPhase);
        _scheduler.Received(1).EnqueueUserNotifications("user-a");
    }

    [Fact]
    public async Task DiffIsIdempotent_OnlyActionsNotAlreadyStoredAreAdded()
    {
        var process = Pending("user-b", externalId: 999);
        StubPending(process);
        StubExisting(process.Id, StoredAction(process.Id, 5001, 81)); // 5001 already present
        _client.GetFirstPageActionsAsync(999, Arg.Any<CancellationToken>())
            .Returns(RamaResult<List<ActionData>>.Ok(
            [
                Action(5001, 81, "Auto admite"),   // already stored -> must be skipped
                Action(5002, 82, "Fijacion estado") // new -> must be added
            ]));

        await BuildJob().RunAsync();

        await _repo.Received(1).AddActionsAsync(
            Arg.Is<IEnumerable<ProcessAction>>(a =>
                a.Count() == 1 && a.Single().ExternalActionId == 5002),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WhenNoNewActions_DoesNotAddOrNotify()
    {
        var process = Pending("user-c", externalId: 999);
        process.Attended = true;
        StubPending(process);
        StubExisting(process.Id, StoredAction(process.Id, 5002, 82));
        _client.GetFirstPageActionsAsync(999, Arg.Any<CancellationToken>())
            .Returns(RamaResult<List<ActionData>>.Ok([Action(5002, 82, "Fijacion estado")]));

        await BuildJob().RunAsync();

        await _repo.DidNotReceive().AddActionsAsync(
            Arg.Any<IEnumerable<ProcessAction>>(), Arg.Any<CancellationToken>());
        _scheduler.DidNotReceive().EnqueueUserNotifications(Arg.Any<string>());
        Assert.True(process.Attended);
        Assert.Equal(ProcessSyncPhase.Idle, process.SyncPhase);
    }

    [Fact]
    public async Task When403_SetsCooldown_Reschedules_LeavesProcessPending()
    {
        var process = Pending("user-d", externalId: 999);
        StubPending(process);
        _client.GetFirstPageActionsAsync(999, Arg.Any<CancellationToken>())
            .Returns(RamaResult<List<ActionData>>.Fail(FailureKind.WafBlocked, "WAF 403"));

        await BuildJob().RunAsync();

        await _syncState.Received(1).SetWafBlockedUntilAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        _scheduler.Received(1).ScheduleActionsSweep(Arg.Any<TimeSpan>());
        Assert.Equal(ProcessSyncPhase.PendingActions, process.SyncPhase);
    }

    [Fact]
    public async Task PacesBetweenProcesses_ButNotBeforeTheFirst()
    {
        var p1 = Pending("user-1", externalId: 999);
        var p2 = Pending("user-2", externalId: 998);
        StubPending(p1, p2);
        StubExisting(p1.Id);
        StubExisting(p2.Id);
        _client.GetFirstPageActionsAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(RamaResult<List<ActionData>>.Ok([]));

        await BuildJob().RunAsync();

        await _delay.Received(1).WaitAsync(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>());
    }

    // ── helpers ────────────────────────────────────────────────────────────────

    private void StubPending(params Process[] processes) =>
        _repo.GetPendingActionsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(processes.ToList());

    private void StubExisting(Guid processId, params ProcessAction[] actions) =>
        _repo.GetActionsAsync(processId, Arg.Any<CancellationToken>()).Returns(actions.ToList());

    private static Process Pending(string userId, long externalId) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        FileNumber = "11001310300120230001200",
        ExternalProcessId = externalId,
        SyncPhase = ProcessSyncPhase.PendingActions,
        LastExternalConsecutive = 80,
        Attended = true,
    };

    private static ProcessAction StoredAction(Guid processId, long externalId, int consecutive) => new()
    {
        Id = Guid.NewGuid(),
        ProcessId = processId,
        ExternalActionId = externalId,
        ConsecutiveNumber = consecutive,
        Action = "Fijacion estado",
        RecordedAt = DateOnly.FromDateTime(Recorded),
    };

    private static ActionData Action(long id, int consecutive, string type) =>
        new(id, consecutive, Recorded, type, null, null, null, Recorded, null, false);

    private ActionsSweepJob BuildJob() => new(
        _repo, _client, _syncState, _scheduler, _delay,
        Options.Create(new SweepOptions { BatchSize = 100, MinimumHoursBetweenSyncsPerProcess = 22 }),
        Options.Create(new ThrottleOptions()),
        Options.Create(new WafOptions()),
        _clock,
        NullLogger<ActionsSweepJob>.Instance);
}
