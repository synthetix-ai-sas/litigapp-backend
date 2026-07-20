using System.Text;
using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Notifications.Dtos;

namespace LitigApp.Infrastructure.Imports;

/// <summary>
/// Builds "procesos_con_errores.csv" from import_jobs.errors (blueprint §9). Shared verbatim
/// by the ImportComplete email attachment and GET /imports/{id}/errors.csv, so both are
/// byte-for-byte identical. UTF-8 with BOM (Excel/Windows accent rendering), RFC 4180
/// quoting, and CSV-injection sanitization — the source rows come from a user-uploaded Excel,
/// so radicado/message values are treated as untrusted.
/// </summary>
internal sealed class ImportErrorsCsvBuilder : IImportErrorsCsvBuilder
{
    // Known error codes -> canonical, user-facing Spanish message. Codes not listed here fall
    // back to whatever message was stored on the row (still readable, just less polished).
    private static readonly Dictionary<string, string> CodeMessages = new()
    {
        ["INVALID_RADICADO"] = "Radicado inválido: no tiene 23 dígitos",
        ["PROCESS_NOT_FOUND_IN_RAMA"] = "No encontrado en la Rama Judicial",
        ["COURT_NOT_FOUND"] = "Despacho no encontrado en el catálogo",
        ["PROCESS_DATA_INCOMPLETE"] = "Datos incompletos devueltos por la Rama Judicial",
        ["INVALID_FILE_NUMBER"] = "Radicado con formato inválido",
        ["RAMA_OVERVIEW_FAILED"] = "No se pudo consultar en la Rama Judicial",
    };

    // Cells starting with these characters can be interpreted as formulas by Excel/Sheets
    // when the CSV is opened — a classic CSV-injection vector. Prefixing with ' neutralizes it
    // while keeping the visible text intact.
    private static readonly char[] DangerousPrefixes = ['=', '+', '-', '@'];

    public byte[] Build(IReadOnlyList<ImportErrorRow> errors)
    {
        var sb = new StringBuilder();
        sb.Append("Fila,Radicado,Motivo\r\n");

        foreach (var row in errors)
        {
            var motivo = CodeMessages.TryGetValue(row.Code, out var canonical) ? canonical : row.Message;

            sb.Append(row.Row).Append(',');
            sb.Append(FormatCell(row.Radicado)).Append(',');
            sb.Append(FormatCell(motivo)).Append("\r\n");
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return [.. Encoding.UTF8.GetPreamble(), .. bytes];
    }

    private static string FormatCell(string value)
    {
        var sanitized = value.Length > 0 && DangerousPrefixes.Contains(value[0])
            ? "'" + value
            : value;

        // RFC 4180: quote (and double any embedded quotes) when the value contains a comma,
        // quote, or newline.
        if (sanitized.IndexOfAny([',', '"', '\r', '\n']) >= 0)
            return "\"" + sanitized.Replace("\"", "\"\"") + "\"";

        return sanitized;
    }
}
