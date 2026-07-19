using System.Net;

namespace LitigApp.Application.Features.Notifications;

/// <summary>
/// Builds the render model for UserDigestTemplate.html (blueprint §10.4). Free-text values
/// sourced from the Rama Judicial API (lawyer name, action type/note) are HTML-escaped here
/// so the Scriban renderer never needs to know about escaping. Radicado and the formatted
/// date are validated/system-controlled shapes (digits, ISO date) that can't carry HTML
/// metacharacters, so they're passed through unescaped.
/// </summary>
public static class UserDigestEmailModelBuilder
{
    public static IReadOnlyDictionary<string, object?> Build(
        string lawyerFullName, DigestCut cut, string dashboardUrl, int year) =>
        new Dictionary<string, object?>
        {
            ["AbogadoNombre"] = WebUtility.HtmlEncode(lawyerFullName),
            ["processes"] = cut.Shown.Select(p => new Dictionary<string, object?>
            {
                ["radicado"] = p.FileNumber,
                ["fecha"] = p.LastCourtActionAt.ToString("yyyy-MM-dd"),
                ["accion"] = WebUtility.HtmlEncode(p.LatestAction ?? string.Empty),
                ["anotacion"] = WebUtility.HtmlEncode(p.LatestAnnotation ?? string.Empty),
            }).ToList(),
            ["remaining"] = cut.Remaining,
            ["UrlDashboard"] = dashboardUrl,
            ["Año"] = year,
        };

    public static string BuildSubject(int totalProcessesChanged) => totalProcessesChanged == 1
        ? "Tienes 1 novedad en tus procesos — LitigApp"
        : $"Tienes {totalProcessesChanged} novedades en tus procesos — LitigApp";
}
