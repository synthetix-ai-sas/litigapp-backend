namespace LitigApp.Api.Features.Imports;

public sealed record ExcelColumnResponse(string Key, string? Header);

/// <summary>
/// Preview response: the cached <c>previewId</c> (passed back to execute), the columns to map,
/// a capped sample of rows for display, and the full data-row count.
/// </summary>
public sealed record ImportPreviewResponse(
    Guid PreviewId,
    IReadOnlyList<ExcelColumnResponse> Columns,
    IReadOnlyList<IReadOnlyDictionary<string, string?>> Rows,
    int TotalRows);

/// <summary>Request body for POST /imports (execute after preview + column mapping).</summary>
public sealed record ExecuteImportRequest(Guid PreviewId, ColumnMappingRequest Mapping, string? FileName);

public sealed record ColumnMappingRequest(string RadicadoCol, string? NotesCol);

/// <summary>Response for POST /imports (202 Accepted) — frontend starts polling /active.</summary>
public sealed record ExecuteImportResponse(Guid ImportJobId, string Status);

/// <summary>
/// Unified response for GET /imports/active and GET /imports/{id}.
/// Mirrors the import_jobs polling fields the frontend needs.
/// </summary>
public sealed record ImportJobResponse(
    Guid Id,
    string FileName,
    int TotalRows,
    int ProcessedRows,
    int SuccessCount,
    int ErrorCount,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    string? Errors);
