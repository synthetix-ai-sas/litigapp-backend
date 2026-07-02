using ClosedXML.Excel;
using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Imports;

namespace LitigApp.Infrastructure.Imports;

/// <summary>
/// Parses .xlsx with ClosedXML (UTF-8 internally — no accent corruption). Row 1 = headers,
/// rows 2..N = data, keyed by column letter. Loads the DOM in memory; the 2 MB / 5000-row
/// caps (enforced by the endpoint) keep that bounded (blueprint §9 Step 9).
/// </summary>
internal sealed class ClosedXmlExcelParser : IExcelParser
{
    public ExcelPreview Parse(Stream stream, string fileName)
    {
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheets.FirstOrDefault()
            ?? throw new InvalidOperationException("El archivo no contiene hojas.");

        var used = ws.RangeUsed();
        if (used is null)
            return new ExcelPreview(fileName, [], []);

        var firstRow = used.FirstRow().RowNumber();
        var lastRow = used.LastRow().RowNumber();
        var firstCol = used.FirstColumn().ColumnNumber();
        var lastCol = used.LastColumn().ColumnNumber();

        var columns = new List<ExcelColumn>(lastCol - firstCol + 1);
        for (var c = firstCol; c <= lastCol; c++)
        {
            var letter = XLHelper.GetColumnLetterFromNumber(c);
            var header = ws.Cell(firstRow, c).GetString().Trim();
            columns.Add(new ExcelColumn(letter, string.IsNullOrEmpty(header) ? null : header));
        }

        var rows = new List<IReadOnlyDictionary<string, string?>>(Math.Max(0, lastRow - firstRow));
        for (var r = firstRow + 1; r <= lastRow; r++)
        {
            var cells = new Dictionary<string, string?>(columns.Count);
            foreach (var col in columns)
            {
                var raw = ws.Cell(r, XLHelper.GetColumnNumberFromLetter(col.Key)).GetString();
                cells[col.Key] = string.IsNullOrWhiteSpace(raw) ? null : raw.Trim();
            }

            rows.Add(cells);
        }

        return new ExcelPreview(fileName, columns, rows);
    }
}
