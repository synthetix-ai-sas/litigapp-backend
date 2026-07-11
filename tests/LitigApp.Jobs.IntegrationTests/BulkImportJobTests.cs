using System.Text.Json;
using FluentAssertions;
using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Imports;
using LitigApp.Application.Features.Processes.Dtos;
using LitigApp.Domain.Common;
using LitigApp.Domain.Imports;
using LitigApp.Infrastructure.Persistence;
using LitigApp.Infrastructure.Persistence.Repositories;
using LitigApp.Infrastructure.Sync;
using LitigApp.Infrastructure.Time;
using LitigApp.Jobs;
using LitigApp.Jobs.ProcessSyncJobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;

namespace LitigApp.Jobs.IntegrationTests;

/// <summary>
/// Acceptance tests for the BulkImportJob radicado-first strategy (blueprint §9).
/// Uses a fake IProcessCreator to isolate from the Rama Judicial API.
/// </summary>
public sealed class BulkImportJobTests : IAsyncLifetime
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
    public async Task Row_With23DigitRadicado_IsImported_SuccessCountIncremented()
    {
        var job = await SetupAndRun(
            [Row("11001310300120230001200")]);  // valid 23-digit

        job.Status.Should().Be(ImportStatus.Completed);
        job.SuccessCount.Should().Be(1);
        job.ErrorCount.Should().Be(0);
    }

    [Fact]
    public async Task NullPreviewPayload_FailsJob_WithPreviewMissing()
    {
        var job = new ImportJob
        {
            Id = Guid.NewGuid(),
            UserId = "user-bulk",
            FileName = "portafolio.xlsx",
            Status = ImportStatus.Pending,
            PreviewId = Guid.NewGuid(),
            PreviewPayload = null,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var updated = await PersistAndRun(job);

        updated.Status.Should().Be(ImportStatus.Failed);
        updated.SyncError.Should().Be("preview_missing");
    }

    [Fact]
    public async Task Row_WithSicRadicado_IsRejected_WithInvalidRadicadoCode()
    {
        var job = await SetupAndRun(
            [Row("25-463759-0")]);  // SIC format, not 23 digits

        job.Status.Should().Be(ImportStatus.Completed);
        job.ErrorCount.Should().Be(1);
        job.SuccessCount.Should().Be(0);
        job.Errors.Should().Contain("INVALID_RADICADO");
    }

    [Fact]
    public async Task Row_With21DigitPenalRadicado_IsRejected()
    {
        var job = await SetupAndRun(
            [Row("210016000001202310001")]);  // Fiscalía — 21 digits

        job.ErrorCount.Should().Be(1);
        job.Errors.Should().Contain("INVALID_RADICADO");
    }

    [Fact]
    public async Task EmptyRow_IsSkipped_Silently_NeitherSuccessNorError()
    {
        var job = await SetupAndRun([
            Row(""),                        // blank → skip
            Row("11001310300120230001201"), // valid
        ]);

        job.Status.Should().Be(ImportStatus.Completed);
        job.SuccessCount.Should().Be(1);
        job.ErrorCount.Should().Be(0);
    }

    [Fact]
    public async Task SectionHeaderRow_WithText_IsSkipped_Silently()
    {
        // "RADICADO" text → after stripping non-digits = "" → skip silently.
        var job = await SetupAndRun([
            Row("RADICADO"),
            Row("11001310300120230001202"),
        ]);

        job.SuccessCount.Should().Be(1);
        job.ErrorCount.Should().Be(0);
    }

    [Fact]
    public async Task MixedRows_ProcessesEachCorrectly()
    {
        var job = await SetupAndRun([
            Row("11001310300120230001203"), // ✅
            Row("25-463759-0"),             // ❌ SIC
            Row(""),                        // ⏭ skip
            Row("11001310300120230001204"), // ✅
        ]);

        job.Status.Should().Be(ImportStatus.Completed);
        job.SuccessCount.Should().Be(2);
        job.ErrorCount.Should().Be(1);
    }

    [Fact]
    public async Task DuplicateRadicado_IsSkippedSilently_NotCountedAsError()
    {
        var job = await SetupAndRun(
            [Row("11001310300120230001200")],
            creatorReturns: "DUPLICATE_PROCESS");

        job.Status.Should().Be(ImportStatus.Completed);
        job.SuccessCount.Should().Be(0);
        job.ErrorCount.Should().Be(0);  // duplicate = silent skip
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static Dictionary<string, string?> Row(string radicado) =>
        new() { ["A"] = radicado };

    private async Task<ImportJob> SetupAndRun(
        IReadOnlyList<Dictionary<string, string?>> rows,
        string? creatorReturns = null)
    {
        var preview = new ExcelPreview(
            "portafolio.xlsx",
            [new ExcelColumn("A", "Radicado")],
            rows.Select(r => (IReadOnlyDictionary<string, string?>)r).ToList());
        var importRows = ImportPreviewProjection.Project(preview, radicadoCol: "A", notesCol: null);

        var job = new ImportJob
        {
            Id = Guid.NewGuid(),
            UserId = "user-bulk",
            FileName = "portafolio.xlsx",
            TotalRows = rows.Count,
            Status = ImportStatus.Pending,
            PreviewId = Guid.NewGuid(),
            PreviewPayload = JsonSerializer.Serialize(importRows),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        return await PersistAndRun(job, creatorReturns);
    }

    private async Task<ImportJob> PersistAndRun(ImportJob job, string? creatorReturns = null)
    {
        _db.ImportJobs.Add(job);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var bulkJob = new BulkImportJob(
            new ImportJobRepository(_db),
            new FakeProcessCreator(creatorReturns),
            new SyncStateService(_db, _clock),
            new RecordingSyncJobScheduler(),
            new NoOpSyncDelay(),
            new OutboxRepository(_db),
            Options.Create(new ThrottleOptions()),
            Options.Create(new WafOptions()),
            _clock,
            NullLogger<BulkImportJob>.Instance);

        await bulkJob.RunAsync(job.Id);

        return await _db.ImportJobs.AsNoTracking().FirstAsync(j => j.Id == job.Id);
    }

    private sealed class FakeProcessCreator(string? failWith = null) : IProcessCreator
    {
        public Task<Result<ProcessDetailDto>> CreateAsync(
            string userId, string fileNumber, string? alias, CancellationToken ct)
        {
            if (failWith is not null)
                return Task.FromResult(Result<ProcessDetailDto>.Failure(failWith));

            var dto = new ProcessDetailDto(
                Guid.NewGuid(), fileNumber, null, null, null,
                null, null, null, null, null,
                true, "ok", "idle", false, false, [], []);

            return Task.FromResult(Result<ProcessDetailDto>.Success(dto));
        }
    }
}
