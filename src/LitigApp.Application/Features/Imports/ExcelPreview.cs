namespace LitigApp.Application.Features.Imports;

/// <summary>A spreadsheet column: its letter key (e.g. "B") and header text from row 1.</summary>
public sealed record ExcelColumn(string Key, string? Header);

/// <summary>
/// Parsed .xlsx preview. Rows are keyed by column letter (so the user's column mapping —
/// e.g. radicadoCol="B" — resolves directly). Row 1 is treated as the header.
/// </summary>
public sealed record ExcelPreview(
    string FileName,
    IReadOnlyList<ExcelColumn> Columns,
    IReadOnlyList<IReadOnlyDictionary<string, string?>> Rows);
