using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Imports;
using Microsoft.Extensions.Options;

namespace LitigApp.Api.Features.Imports;

public static class ImportsEndpoints
{
    /// <summary>How many data rows the preview response echoes back for display (the full set is cached).</summary>
    private const int PreviewSampleSize = 50;

    public static IEndpointRouteBuilder MapImportsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/imports")
            .RequireAuthorization()
            .WithTags("Imports");

        group.MapPost("/preview", Preview)
            .WithName("PreviewImport")
            .WithSummary("Sube un .xlsx, valida límites de tamaño/filas y devuelve un preview cacheado para mapear columnas")
            .DisableAntiforgery()
            .Produces<ImportPreviewResponse>()
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return app;
    }

    private static IResult Preview(
        IFormFile? file,
        IExcelParser parser,
        IImportPreviewCache cache,
        IOptionsSnapshot<ImportOptions> options)
    {
        var opts = options.Value;

        // Size guard BEFORE parsing (blueprint §9: validate size before ClosedXML loads the DOM).
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

        var columns = preview.Columns
            .Select(c => new ExcelColumnResponse(c.Key, c.Header))
            .ToList();
        var sample = preview.Rows.Take(PreviewSampleSize).ToList();

        return TypedResults.Ok(new ImportPreviewResponse(previewId, columns, sample, preview.Rows.Count));
    }
}
