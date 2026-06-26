using LitigApp.Application.Common.Abstractions;
using LitigApp.Domain.Processes;
using LitigApp.Jobs;
using LitigApp.Jobs.ProcessSyncJobs;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace LitigApp.Jobs.UnitTests;

public class CompletePartialFetchJobUnitTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTime Recorded = new(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc);

    private readonly IProcessRepository _repo = Substitute.For<IProcessRepository>();
    private readonly IRamaJudicialClient _client = Substitute.For<IRamaJudicialClient>();
    private readonly ISyncStateService _syncState = Substitute.For<ISyncStateService>();
    private readonly ISyncJobScheduler _scheduler = Substitute.For<ISyncJobScheduler>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    public CompletePartialFetchJobUnitTests() => _clock.UtcNow.Returns(Now.UtcDateTime);

    [Fact]
    public async Task WhenCooldownActive_Reschedules_WithoutLoadingProcess()
    {
        var id = Guid.NewGuid();
        _syncState.GetWafBlockedUntilAsync(Arg.Any<CancellationToken>()).Returns(Now.AddMinutes(10));

        await BuildJob().RunAsync(id);

        _scheduler.Received(1).SchedulePartialFetch(id, Arg.Any<TimeSpan>());
        await _repo.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WhenProcessNotPendingPartial_DoesNothing()
    {
        var process = PartialProcess();
        process.SyncPhase = ProcessSyncPhase.Idle; // already completed
        StubProcess(process);

        await BuildJob().RunAsync(process.Id);

        await _client.DidNotReceive().GetDetailAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        await _client.DidNotReceive().GetSubjectsAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        await _client.DidNotReceive().GetFirstPageActionsAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WhenAllMissing_FetchesAll_MarksIdleOk()
    {
        var process = PartialProcess();      // ProcessClass null -> detail missing
        StubProcess(process);
        _repo.HasSubjectsAsync(process.Id, Arg.Any<CancellationToken>()).Returns(false);
        _repo.GetActionsAsync(process.Id, Arg.Any<CancellationToken>()).Returns(new List<ProcessAction>());

        _client.GetDetailAsync(999, Arg.Any<CancellationToken>()).Returns(RamaResult<ProcessDetailData?>.Ok(Detail()));
        _client.GetSubjectsAsync(999, Arg.Any<CancellationToken>())
            .Returns(RamaResult<List<SubjectData>>.Ok([new SubjectData(1, "Demandante", false, "123", "Acme")]));
        _client.GetFirstPageActionsAsync(999, Arg.Any<CancellationToken>())
            .Returns(RamaResult<List<ActionData>>.Ok([Action(5001, 81, "Auto admite")]));

        await BuildJob().RunAsync(process.Id);

        Assert.Equal(ProcessSyncPhase.Idle, process.SyncPhase);
        Assert.Equal(ProcessSyncStatus.Ok, process.SyncStatus);
        Assert.Equal("Ejecutivo", process.ProcessClass);
        await _repo.Received(1).AddSubjectsAsync(
            Arg.Is<IEnumerable<ProcessSubject>>(s => s.Count() == 1), Arg.Any<CancellationToken>());
        await _repo.Received(1).AddActionsAsync(
            Arg.Is<IEnumerable<ProcessAction>>(a => a.Count() == 1), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WhenOnlyActionsMissing_FetchesOnlyActions()
    {
        var process = PartialProcess();
        process.ProcessClass = "Ejecutivo"; // detail already present
        StubProcess(process);
        _repo.HasSubjectsAsync(process.Id, Arg.Any<CancellationToken>()).Returns(true);  // subjects present
        _repo.GetActionsAsync(process.Id, Arg.Any<CancellationToken>()).Returns(new List<ProcessAction>()); // actions missing
        _client.GetFirstPageActionsAsync(999, Arg.Any<CancellationToken>())
            .Returns(RamaResult<List<ActionData>>.Ok([Action(5001, 81, "Auto admite")]));

        await BuildJob().RunAsync(process.Id);

        await _client.DidNotReceive().GetDetailAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        await _client.DidNotReceive().GetSubjectsAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        await _client.Received(1).GetFirstPageActionsAsync(999, Arg.Any<CancellationToken>());
        await _repo.DidNotReceive().AddSubjectsAsync(
            Arg.Any<IEnumerable<ProcessSubject>>(), Arg.Any<CancellationToken>());
        await _repo.Received(1).AddActionsAsync(
            Arg.Is<IEnumerable<ProcessAction>>(a => a.Count() == 1), Arg.Any<CancellationToken>());
        Assert.Equal(ProcessSyncPhase.Idle, process.SyncPhase);
    }

    [Fact]
    public async Task When403_SetsCooldown_Reschedules_StaysPending()
    {
        var process = PartialProcess();
        StubProcess(process);
        _client.GetDetailAsync(999, Arg.Any<CancellationToken>())
            .Returns(RamaResult<ProcessDetailData?>.Fail(FailureKind.WafBlocked, "WAF 403"));

        await BuildJob().RunAsync(process.Id);

        await _syncState.Received(1).SetWafBlockedUntilAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        _scheduler.Received(1).SchedulePartialFetch(process.Id, Arg.Any<TimeSpan>());
        Assert.Equal(ProcessSyncPhase.PendingPartialCompletion, process.SyncPhase);
        await _client.DidNotReceive().GetSubjectsAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    // ── helpers ────────────────────────────────────────────────────────────────

    private void StubProcess(Process process) =>
        _repo.GetByIdAsync(process.Id, Arg.Any<CancellationToken>()).Returns(process);

    private static Process PartialProcess() => new()
    {
        Id = Guid.NewGuid(),
        UserId = "user-1",
        FileNumber = "11001310300120230001200",
        ExternalProcessId = 999,
        SyncPhase = ProcessSyncPhase.PendingPartialCompletion,
        SyncStatus = ProcessSyncStatus.Partial,
    };

    private static ProcessDetailData Detail() =>
        new("110010000000", "Juzgado", 1, false, null, "De Ejecución", "Ejecutivo", "Singular", "Sin recurso", "Juez", "Contenido");

    private static ActionData Action(long id, int consecutive, string type) =>
        new(id, consecutive, Recorded, type, null, null, null, Recorded, null, false);

    private CompletePartialFetchJob BuildJob() => new(
        _repo, _client, _syncState, _scheduler,
        Options.Create(new WafOptions()),
        _clock,
        NullLogger<CompletePartialFetchJob>.Instance);
}
