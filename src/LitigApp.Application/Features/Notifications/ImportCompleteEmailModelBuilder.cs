using System.Net;

namespace LitigApp.Application.Features.Notifications;

/// <summary>Builds the render model for ImportCompleteTemplate.html (blueprint §10.4).</summary>
public static class ImportCompleteEmailModelBuilder
{
    public static IReadOnlyDictionary<string, object?> Build(
        string lawyerFullName, string fileName, int totalImported, int totalDuplicates,
        int totalErrors, string dashboardUrl, int year) =>
        new Dictionary<string, object?>
        {
            ["AbogadoNombre"] = WebUtility.HtmlEncode(lawyerFullName),
            ["NombreArchivo"] = WebUtility.HtmlEncode(fileName),
            ["TotalImportados"] = totalImported,
            ["TotalDuplicados"] = totalDuplicates,
            ["TotalErrores"] = totalErrors,
            ["TieneErrores"] = totalErrors > 0,
            ["UrlDashboard"] = dashboardUrl,
            ["Año"] = year,
        };

    public static string BuildSubject(int successCount) =>
        $"Importación completada — {successCount} procesos cargados";
}
