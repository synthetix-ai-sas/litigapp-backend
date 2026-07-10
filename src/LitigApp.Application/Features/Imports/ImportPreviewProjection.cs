namespace LitigApp.Application.Features.Imports;

/// <summary>
/// A single row of a confirmed import, reduced to just the mapped columns.
/// This is what gets persisted on the ImportJob (blueprint §9, spec A1) so the
/// worker can run the import without the API-local preview cache.
/// </summary>
public sealed record ImportRow(string? Radicado, string? Notes);

/// <summary>
/// Projects a parsed <see cref="ExcelPreview"/> down to the mapped columns
/// (radicado + optional notes), preserving row order. Order matters: the
/// worker reports per-row errors by position (Excel row = index + 2).
/// </summary>
public static class ImportPreviewProjection
{
    public static IReadOnlyList<ImportRow> Project(
        ExcelPreview preview, string radicadoCol, string? notesCol) =>
        preview.Rows
            .Select(row => new ImportRow(
                Value(row, radicadoCol),
                notesCol is null ? null : Value(row, notesCol)))
            .ToList();

    private static string? Value(IReadOnlyDictionary<string, string?> row, string col) =>
        row.TryGetValue(col, out var v) ? v : null;
}
