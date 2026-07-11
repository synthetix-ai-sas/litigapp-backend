using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Processes;
using LitigApp.Application.Features.Processes.Dtos;
using LitigApp.Application.Features.Processes.Services;
using LitigApp.Domain.Processes;
using NSubstitute;

namespace LitigApp.Application.UnitTests.Features.Processes;

public class ProcessCreationServiceTests
{
    private const string ValidFileNumber = "17001400301020240019200";

    private readonly IProcessRepository _repo = Substitute.For<IProcessRepository>();
    private readonly IRamaJudicialClient _rama = Substitute.For<IRamaJudicialClient>();
    private readonly IProcessReader _reader = Substitute.For<IProcessReader>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly IPartialFetchScheduler _scheduler = Substitute.For<IPartialFetchScheduler>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    public ProcessCreationServiceTests()
    {
        _currentUser.UserId.Returns("u1");
        _clock.UtcNow.Returns(new DateTime(2026, 6, 12, 0, 0, 0, DateTimeKind.Utc));
        _reader.GetByIdAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(DummyDetail());
    }

    private ProcessCreationService CreateSut() =>
        new(_repo, _rama, _reader, _clock, _scheduler, _currentUser);

    private void StubOverviewOk() =>
        _rama.GetOverviewByFileNumberAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(RamaResult<OverviewData?>.Ok(new OverviewData(
                999, 7, "key", new DateTime(2026, 3, 20), "JUZGADO 002", "Caldas", false)));

    private void StubDetailOk() =>
        _rama.GetDetailAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(RamaResult<ProcessDetailData?>.Ok(new ProcessDetailData(
                "170014003010", "JUZGADO 002", 7, false, null,
                "De Ejecución", "Ejecutivo Singular", "Por sumas", "Sin recurso", "Juez X", "contenido")));

    private void StubSubjectsOk() =>
        _rama.GetSubjectsAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(RamaResult<List<SubjectData>>.Ok(new List<SubjectData>
            {
                new(1, "Demandante", false, "123", "OSCAR ORTIZ"),
            }));

    private void StubActionsOk() =>
        _rama.GetFirstPageActionsAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(RamaResult<List<ActionData>>.Ok(new List<ActionData>
            {
                new(10, 82, new DateTime(2026, 3, 20), "Fijacion estado", null, null, null, null, null, false),
            }));

    [Fact]
    public async Task CreateAsync_AllCallsSucceed_PersistsOkAndDoesNotSchedule()
    {
        StubOverviewOk();
        StubDetailOk();
        StubSubjectsOk();
        StubActionsOk();

        var result = await CreateSut().CreateAsync(ValidFileNumber, "alias", default);

        Assert.True(result.IsSuccess);
        await _repo.Received(1).AddAsync(
            Arg.Is<Process>(p =>
                p.SyncStatus == "ok" &&
                p.SyncPhase == "idle" &&
                p.Attended &&
                p.LastExternalConsecutive == 82 &&
                p.CurrentStatus == "Fijacion estado" &&
                p.Subjects.Count == 1 &&
                p.Actions.Count == 1),
            Arg.Any<CancellationToken>());
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        _scheduler.DidNotReceive().SchedulePartialCompletion(Arg.Any<Guid>());
    }

    [Fact]
    public async Task CreateAsync_PostOverviewFailure_PersistsPartialAndSchedules()
    {
        StubOverviewOk();
        StubSubjectsOk();
        StubActionsOk();
        // detail fails after Polly
        _rama.GetDetailAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(RamaResult<ProcessDetailData?>.Fail(FailureKind.Transient, "boom"));

        var result = await CreateSut().CreateAsync(ValidFileNumber, null, default);

        Assert.True(result.IsSuccess);
        await _repo.Received(1).AddAsync(
            Arg.Is<Process>(p =>
                p.SyncStatus == "partial" &&
                p.SyncPhase == "pending_partial_completion" &&
                p.SyncError != null),
            Arg.Any<CancellationToken>());
        _scheduler.Received(1).SchedulePartialCompletion(Arg.Any<Guid>());
    }

    [Fact]
    public async Task CreateAsync_OverviewReturnsNull_FailsNotFoundInRama()
    {
        _rama.GetOverviewByFileNumberAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(RamaResult<OverviewData?>.Ok(null));

        var result = await CreateSut().CreateAsync(ValidFileNumber, null, default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ProcessErrorCodes.ProcessNotFoundInRama, result.Error);
        await _repo.DidNotReceive().AddAsync(Arg.Any<Process>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_OverviewCallFails_FailsRamaOverviewFailed()
    {
        _rama.GetOverviewByFileNumberAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(RamaResult<OverviewData?>.Fail(FailureKind.WafBlocked, "403"));

        var result = await CreateSut().CreateAsync(ValidFileNumber, null, default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ProcessErrorCodes.RamaOverviewFailed, result.Error);
    }

    [Fact]
    public async Task CreateAsync_DuplicateProcess_Fails()
    {
        _repo.ExistsAsync("u1", ValidFileNumber, Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateSut().CreateAsync(ValidFileNumber, null, default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ProcessErrorCodes.DuplicateProcess, result.Error);
    }

    [Fact]
    public async Task CreateAsync_ImportInProgress_Fails()
    {
        _repo.HasActiveImportAsync("u1", Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateSut().CreateAsync(ValidFileNumber, null, default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ProcessErrorCodes.ImportInProgress, result.Error);
    }

    [Theory]
    [InlineData("123")]               // too short
    [InlineData("1700140030102024001920A")] // non-digit
    public async Task CreateAsync_InvalidFileNumber_Fails(string fileNumber)
    {
        var result = await CreateSut().CreateAsync(fileNumber, null, default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ProcessErrorCodes.InvalidFileNumber, result.Error);
    }

    [Fact]
    public async Task CreateAsync_PrivateProcess_PersistsOverviewOnly_NeverCallsDetailSubjectsActions()
    {
        _rama.GetOverviewByFileNumberAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(RamaResult<OverviewData?>.Ok(new OverviewData(
                888, 7, "key", LastActionDate: null, "JUZGADO 002", "Caldas", IsPrivate: true)));

        var result = await CreateSut().CreateAsync(ValidFileNumber, null, default);

        Assert.True(result.IsSuccess);
        await _repo.Received(1).AddAsync(
            Arg.Is<Process>(p =>
                p.IsPrivate &&
                p.SyncStatus == "ok" &&
                p.SyncPhase == "idle" &&
                p.Attended &&
                p.LastCourtActionAt == null &&
                p.Subjects.Count == 0 &&
                p.Actions.Count == 0),
            Arg.Any<CancellationToken>());
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

        // The other 3 endpoints all 404 for private processes — they must NEVER be called.
        await _rama.DidNotReceive().GetDetailAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        await _rama.DidNotReceive().GetSubjectsAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        await _rama.DidNotReceive().GetFirstPageActionsAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        _scheduler.DidNotReceive().SchedulePartialCompletion(Arg.Any<Guid>());
    }

    [Fact]
    public async Task CreateAsync_SubjectWithBlankOrNullType_IsDiscarded_NotPersisted()
    {
        StubOverviewOk();
        StubDetailOk();
        StubActionsOk();
        _rama.GetSubjectsAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(RamaResult<List<SubjectData>>.Ok(new List<SubjectData>
            {
                new(1, "Demandante", false, "123", "OSCAR ORTIZ"),
                new(2, "", false, "456", "TIPO VACIO"),      // blank subject_type → discard
                new(3, null!, false, "789", "TIPO NULO"),    // null subject_type → discard
            }));

        var result = await CreateSut().CreateAsync(ValidFileNumber, null, default);

        Assert.True(result.IsSuccess);
        await _repo.Received(1).AddAsync(
            Arg.Is<Process>(p =>
                p.Subjects.Count == 1 &&
                p.Subjects.All(s => !string.IsNullOrWhiteSpace(s.SubjectType))),
            Arg.Any<CancellationToken>());
    }

    private static ProcessDetailDto DummyDetail() => new(
        Guid.NewGuid(), ValidFileNumber, null, null, 2024, null, null, null, null, null,
        false, "ok", "idle", false, true,
        new List<ProcessSubjectDto>(), new List<ProcessActionDto>());
}
