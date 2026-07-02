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
