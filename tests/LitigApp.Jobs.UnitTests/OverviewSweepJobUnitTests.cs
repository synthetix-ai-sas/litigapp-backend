using LitigApp.Application.Common.Abstractions;
using LitigApp.Domain.Processes;
using LitigApp.Jobs;
using LitigApp.Jobs.ProcessSyncJobs;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace LitigApp.Jobs.UnitTests;

/// <summary>
/// Fast, DB-free tests of OverviewSweep orchestration branches (cooldown gate, change
/// detection -> enqueue, 403 -> cooldown). Persistence/idempotency realism is covered by
/// the integration tests; these isolate the control flow.
/// </summary>
public class OverviewSweepJobUnitTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);

    private readonly IProcessRepository _repo = Substitute.For<IProcessRepository>();
    private readonly IRamaJudicialClient _client = Substitute.For<IRamaJudicialClient>();
    private readonly ISyncStateService _syncState = Substitute.For<ISyncStateService>();
    private readonly ISyncJobScheduler _scheduler = Substitute.For<ISyncJobScheduler>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    public OverviewSweepJobUnitTests() => _clock.UtcNow.Returns(Now.UtcDateTime);

    [Fact]
    public async Task WhenCooldownActive_SkipsRun_NoQueryNoApiNoEnqueue()
    {
        _syncState.GetWafBlockedUntilAsync(Arg.Any<CancellationToken>()).Returns(Now.AddMinutes(10));

        await BuildJob().RunAsync();

        await _repo.DidNotReceive().GetEligibleForOverviewSweepAsync(
            Arg.Any<int>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>());
        await _client.DidNotReceive().GetOverviewByFileNumberAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        _scheduler.DidNotReceive().EnqueueActionsSweep();
    }

    [Fact]
    public async Task WhenChangeDetected_MarksPendingActions_AndEnqueuesActionsSweep()
    {
        var process = Eligible();
        StubBatch(process);
        _client.GetOverviewByFileNumberAsync(process.FileNumber, Arg.Any<CancellationToken>())
            .Returns(RamaResult<OverviewData?>.Ok(Overview(lastAction: Now.AddDays(-1).UtcDateTime)));

        await BuildJob().RunAsync();

        Assert.Equal(ProcessSyncPhase.PendingActions, process.SyncPhase);
        _scheduler.Received(1).EnqueueActionsSweep();
    }

    [Fact]
    public async Task WhenNoChange_MarksIdle_AndDoesNotEnqueue()
    {
        var sameDate = Now.AddDays(-5);
        var process = Eligible(lastCourtActionAt: sameDate);
        StubBatch(process);
        _client.GetOverviewByFileNumberAsync(process.FileNumber, Arg.Any<CancellationToken>())
            .Returns(RamaResult<OverviewData?>.Ok(Overview(lastAction: sameDate.UtcDateTime)));

        await BuildJob().RunAsync();

        Assert.Equal(ProcessSyncPhase.Idle, process.SyncPhase);
        _scheduler.DidNotReceive().EnqueueActionsSweep();
    }

    [Fact]
    public async Task When403_SetsCooldown_AndDoesNotEnqueue()
    {
        var process = Eligible();
        StubBatch(process);
        _client.GetOverviewByFileNumberAsync(process.FileNumber, Arg.Any<CancellationToken>())
            .Returns(RamaResult<OverviewData?>.Fail(FailureKind.WafBlocked, "WAF 403"));

        await BuildJob().RunAsync();

        await _syncState.Received(1).SetWafBlockedUntilAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        _scheduler.DidNotReceive().EnqueueActionsSweep();
    }

    // ── helpers ────────────────────────────────────────────────────────────────

    private void StubBatch(params Process[] processes) =>
        _repo.GetEligibleForOverviewSweepAsync(Arg.Any<int>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(processes.ToList());

    private static Process Eligible(DateTimeOffset? lastCourtActionAt = null) => new()
    {
        Id = Guid.NewGuid(),
        UserId = "user-1",
        FileNumber = "11001310300120230001200",
        SyncPhase = ProcessSyncPhase.Idle,
        ExternalProcessId = 999,
        LastCourtActionAt = lastCourtActionAt ?? new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero),
    };

    private static OverviewData Overview(DateTime lastAction) =>
        new(999, 1, "11001310300120230001200", lastAction, "Juzgado", "Bogotá", false);

    private OverviewSweepJob BuildJob() => new(
        _repo, _client, _syncState, _scheduler,
        Options.Create(new SweepOptions { BatchSize = 100, MinimumHoursBetweenSyncsPerProcess = 22 }),
        Options.Create(new WafOptions()),
        _clock,
        NullLogger<OverviewSweepJob>.Instance);
}
