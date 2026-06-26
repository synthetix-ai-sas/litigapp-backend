using FluentAssertions;
using LitigApp.Application.Common.Abstractions;
using LitigApp.Domain.Processes;
using LitigApp.Infrastructure.Persistence;
using LitigApp.Infrastructure.Persistence.Repositories;
using LitigApp.Infrastructure.Sync;
using LitigApp.Infrastructure.Time;
using LitigApp.Jobs.ProcessSyncJobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;

namespace LitigApp.Jobs.IntegrationTests;

public sealed class CompletePartialFetchJobTests : IAsyncLifetime
{
    private static readonly DateTime Recorded = new(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc);

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();
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
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task CompletesMissingEndpoints_PersistsGraph_AndMarksIdle()
    {
        var process = new Process
        {
            Id = Guid.NewGuid(),
            UserId = "user-1",
            FileNumber = "11001310300120230001200",
            ExternalProcessId = 999,
            SyncPhase = ProcessSyncPhase.PendingPartialCompletion,
            SyncStatus = ProcessSyncStatus.Partial,
            ProcessClass = null, // detail missing
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            UpdatedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
        };
        _db.Processes.Add(process);
        await _db.SaveChangesAsync(CancellationToken.None);
        _db.ChangeTracker.Clear(); // the job runs in a fresh scope — don't reuse tracked state

        var client = new FakePartialClient(
            detail: RamaResult<ProcessDetailData?>.Ok(new ProcessDetailData(
                "110010000000", "Juzgado", 1, false, null, "De Ejecución", "Ejecutivo", "Singular", "Sin recurso", "Juez", "Contenido")),
            subjects: RamaResult<List<SubjectData>>.Ok([new SubjectData(1, "Demandante", false, "123", "Acme")]),
            actions: RamaResult<List<ActionData>>.Ok(
            [
                Action(5001, 81, "Auto admite"),
                Action(5002, 82, "Fijacion estado"),
            ]));

        await BuildJob(client).RunAsync(process.Id);

        var updated = await _db.Processes.AsNoTracking().FirstAsync(p => p.Id == process.Id, CancellationToken.None);
        updated.SyncPhase.Should().Be(ProcessSyncPhase.Idle);
        updated.SyncStatus.Should().Be(ProcessSyncStatus.Ok);
        updated.ProcessClass.Should().Be("Ejecutivo");
        updated.LastExternalConsecutive.Should().Be(82);

        var subjects = await _db.ProcessSubjects.AsNoTracking()
            .CountAsync(s => s.ProcessId == process.Id, CancellationToken.None);
        subjects.Should().Be(1);

        var actions = await _db.ProcessActions.AsNoTracking()
            .Where(a => a.ProcessId == process.Id).OrderBy(a => a.ConsecutiveNumber).ToListAsync(CancellationToken.None);
        actions.Should().HaveCount(2);
        actions.Single(a => a.ConsecutiveNumber == 82).GroupedWithId
            .Should().Be(actions.Single(a => a.ConsecutiveNumber == 81).Id); // grouping persisted via self-ref FK
    }

    private CompletePartialFetchJob BuildJob(IRamaJudicialClient client)
    {
        var clock = new SystemDateTimeProvider();
        return new CompletePartialFetchJob(
            new ProcessRepository(_db),
            client,
            new SyncStateService(_db, clock),
            new RecordingSyncJobScheduler(),
            Options.Create(new LitigApp.Jobs.WafOptions()),
            clock,
            NullLogger<CompletePartialFetchJob>.Instance);
    }

    private static ActionData Action(long id, int consecutive, string type) =>
        new(id, consecutive, Recorded, type, null, null, null, Recorded, null, false);

    private sealed class FakePartialClient(
        RamaResult<ProcessDetailData?> detail,
        RamaResult<List<SubjectData>> subjects,
        RamaResult<List<ActionData>> actions) : IRamaJudicialClient
    {
        public Task<RamaResult<ProcessDetailData?>> GetDetailAsync(long id, CancellationToken ct) =>
            Task.FromResult(detail);

        public Task<RamaResult<List<SubjectData>>> GetSubjectsAsync(long id, CancellationToken ct) =>
            Task.FromResult(subjects);

        public Task<RamaResult<List<ActionData>>> GetFirstPageActionsAsync(long id, CancellationToken ct) =>
            Task.FromResult(actions);

        public Task<RamaResult<OverviewData?>> GetOverviewByFileNumberAsync(string fileNumber, CancellationToken ct) =>
            throw new NotSupportedException();
    }
}
