using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Imports;
using LitigApp.Application.Features.Notifications.Dtos;
using LitigApp.Domain.Imports;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace LitigApp.Api.Features.Imports;

public static class ImportsEndpoints
{
    private const int PreviewSampleSize = 50;

    /// <summary>Seconds after completion that GET /active still returns the job (60s polling window).</summary>
    private const int ActiveWindowSeconds = 60;

    public static IEndpointRouteBuilder MapImportsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/imports")
            .RequireAuthorization()
            .WithTags("Imports");

        group.MapPost("/preview", Preview)
            .WithName("PreviewImport")
            .WithSummary("Sube un .xlsx, valida límites y devuelve preview cacheado para mapear columnas")
            .DisableAntiforgery()
            .Produces<ImportPreviewResponse>()
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("/", Execute)
            .WithName("ExecuteImport")
            .WithSummary("Ejecuta la importación: crea ImportJob y encola BulkImportJob")
            .Produces<ExecuteImportResponse>(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/active", GetActive)
            .WithName("GetActiveImport")
            .WithSummary("Devuelve el job activo del usuario (o el completado en los últimos 60s) para el banner de progreso");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetImportById")
            .WithSummary("Consulta directa de un job (para historial / link desde email)");

        group.MapGet("/{id:guid}/errors.csv", DownloadErrorsCsv)
            .WithName("DownloadImportErrorsCsv")
            .WithSummary("CSV de los procesos que fallaron — mismo builder que el adjunto del email ImportComplete")
            .Produces(StatusCodes.Status200OK, contentType: "text/csv")
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    // ── POST /preview ─────────────────────────────────────────────────────────

    private static IResult Preview(
        IFormFile? file,
        IExcelParser parser,
        IImportPreviewCache cache,
        IOptionsSnapshot<ImportOptions> options)
    {
        var opts = options.Value;

        if (file is null || file.Length == 0)
            return ImportProblem.From(ImportErrorCodes.EmptyFile);
        if (file.Length > opts.MaxFileSizeBytes)
            return ImportProblem.From(ImportErrorCodes.FileTooLarge);

        ExcelPreview preview;
        try
        {
            using var stream = file.OpenReadStream();
            preview = parser.Parse(stream, file.FileName);
        }
        catch (Exception)
        {
            return ImportProblem.From(ImportErrorCodes.InvalidFile);
        }

        if (preview.Rows.Count == 0)
            return ImportProblem.From(ImportErrorCodes.EmptyFile);
        if (preview.Rows.Count > opts.MaxRows)
            return ImportProblem.From(ImportErrorCodes.TooManyRows);

        var previewId = Guid.NewGuid();
        cache.Set(previewId, preview);

        var columns = preview.Columns.Select(c => new ExcelColumnResponse(c.Key, c.Header)).ToList();
        var sample = preview.Rows.Take(PreviewSampleSize).ToList();

        return TypedResults.Ok(new ImportPreviewResponse(previewId, columns, sample, preview.Rows.Count));
    }

    // ── POST / (execute) ──────────────────────────────────────────────────────

    private static async Task<IResult> Execute(
        ExecuteImportRequest request,
        ICurrentUserService currentUser,
        IImportJobRepository importRepo,
        IImportPreviewCache cache,
        ISyncJobScheduler scheduler,
        IDateTimeProvider clock,
        CancellationToken ct)
    {
        var userId = currentUser.UserId!;

        var preview = cache.Get(request.PreviewId);
        if (preview is null)
            return ImportProblem.From(ImportErrorCodes.PreviewExpired);

        var active = await importRepo.GetActiveForUserAsync(userId, ct);
        if (active is not null)
            return ImportProblem.From(ImportErrorCodes.ImportInProgress);

        var mapping = JsonSerializer.Serialize(new
        {
            radicadoCol = request.Mapping.RadicadoCol,
            notesCol    = request.Mapping.NotesCol,
        });

        // Reduce the preview to the mapped columns and persist it on the job, so the worker
        // (separate process, no shared in-memory cache) can run the import. See spec A1 (4.C).
        var importRows = ImportPreviewProjection.Project(
            preview, request.Mapping.RadicadoCol, request.Mapping.NotesCol);

        var job = await importRepo.CreateAsync(new ImportJob
        {
            Id             = Guid.NewGuid(),
            UserId         = userId,
            FileName       = request.FileName ?? preview.FileName,
            TotalRows      = preview.Rows.Count,
            Status         = ImportStatus.Pending,
            ColumnMapping  = mapping,
            PreviewId      = request.PreviewId,
            PreviewPayload = JsonSerializer.Serialize(importRows),
            CreatedAt      = new DateTimeOffset(clock.UtcNow, TimeSpan.Zero),
        }, ct);

        scheduler.EnqueueBulkImport(job.Id);

        return TypedResults.Accepted(
            $"/api/v1/imports/{job.Id}",
            new ExecuteImportResponse(job.Id, job.Status));
    }

    // ── GET /active ───────────────────────────────────────────────────────────

    private static async Task<IResult> GetActive(
        ICurrentUserService currentUser,
        IImportJobRepository importRepo,
        CancellationToken ct)
    {
        var job = await importRepo.GetActiveOrRecentAsync(
            currentUser.UserId!, ActiveWindowSeconds, ct);

        return job is null
            ? TypedResults.NoContent()
            : TypedResults.Ok(ToResponse(job));
    }

    // ── GET /{id} ─────────────────────────────────────────────────────────────

    private static async Task<IResult> GetById(
        Guid id,
        ICurrentUserService currentUser,
        IImportJobRepository importRepo,
        CancellationToken ct)
    {
        var job = await importRepo.GetByIdAsync(id, ct);
        if (job is null || job.UserId != currentUser.UserId)
            return TypedResults.NotFound();

        return TypedResults.Ok(ToResponse(job));
    }

    private static ImportJobResponse ToResponse(ImportJob job) => new(
        job.Id, job.FileName, job.TotalRows, job.ProcessedRows,
        job.SuccessCount, job.ErrorCount, job.Status,
        job.CreatedAt, job.CompletedAt, job.Errors);

    // ── GET /{id}/errors.csv ──────────────────────────────────────────────────

    private static readonly JsonSerializerOptions ErrorsJsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static async Task<IResult> DownloadErrorsCsv(
        Guid id,
        ICurrentUserService currentUser,
        IImportJobRepository importRepo,
        IImportErrorsCsvBuilder csvBuilder,
        CancellationToken ct)
    {
        var job = await importRepo.GetByIdAsync(id, ct);
        if (job is null || job.UserId != currentUser.UserId)
            return TypedResults.NotFound();

        var errors = DeserializeErrors(job.Errors);
        var csvBytes = csvBuilder.Build(errors);

        return TypedResults.File(csvBytes, "text/csv", "procesos_con_errores.csv");
    }

    private static List<ImportErrorRow> DeserializeErrors(string? json)
    {
        if (string.IsNullOrEmpty(json)) return [];
        try { return JsonSerializer.Deserialize<List<ImportErrorRow>>(json, ErrorsJsonOptions) ?? []; }
        catch { return []; }
    }
}
