using System.Net;
using LitigApp.Application.Common.Abstractions;
using LitigApp.Infrastructure.Notifications.Templates;

namespace LitigApp.Infrastructure.UnitTests.Notifications;

public class ScribanEmailTemplateRendererTests
{
    private readonly ScribanEmailTemplateRenderer _sut = new();

    [Fact]
    public void Render_UserDigest_WithLoopAndNoRemaining_HasNoUnresolvedPlaceholders()
    {
        var model = new Dictionary<string, object?>
        {
            ["AbogadoNombre"] = "Sergio",
            ["processes"] = new List<Dictionary<string, object?>>
            {
                new()
                {
                    ["radicado"] = "17001400301020240019200",
                    ["fecha"] = "2026-03-20",
                    ["accion"] = "Fijacion estado",
                    ["anotacion"] = "Actuacion registrada",
                },
            },
            ["remaining"] = 0,
            ["UrlDashboard"] = "https://app.litigapp.co/novelties",
            ["Año"] = 2026,
        };

        var html = _sut.Render(EmailTemplate.UserDigest, model);

        Assert.DoesNotContain("{{", html);
        Assert.Contains("Sergio", html);
        Assert.Contains("17001400301020240019200", html);
        // remaining=0 -> the "+N more" block must not render.
        Assert.DoesNotContain("actuaciones más", html);
    }

    [Fact]
    public void Render_UserDigest_WithRemaining_ShowsMoreLine()
    {
        var model = new Dictionary<string, object?>
        {
            ["AbogadoNombre"] = "Sergio",
            ["processes"] = new List<Dictionary<string, object?>>(),
            ["remaining"] = 2,
            ["UrlDashboard"] = "https://app.litigapp.co/novelties",
            ["Año"] = 2026,
        };

        var html = _sut.Render(EmailTemplate.UserDigest, model);

        Assert.DoesNotContain("{{", html);
        Assert.Contains("2 actuaciones más", html);
    }

    [Fact]
    public void Render_ImportComplete_NoUnresolvedPlaceholders()
    {
        var model = new Dictionary<string, object?>
        {
            ["AbogadoNombre"] = "Sergio",
            ["NombreArchivo"] = "portafolio.xlsx",
            ["TotalImportados"] = 10,
            ["TotalDuplicados"] = 1,
            ["TotalErrores"] = 0,
            ["TieneErrores"] = false,
            ["UrlDashboard"] = "https://app.litigapp.co/processes",
            ["Año"] = 2026,
        };

        var html = _sut.Render(EmailTemplate.ImportComplete, model);

        Assert.DoesNotContain("{{", html);
        Assert.DoesNotContain("Algunos radicados", html); // TieneErrores=false -> block hidden
    }

    [Fact]
    public void Render_PreEscapedValue_PassesThroughWithoutDoubleEscaping()
    {
        var escaped = WebUtility.HtmlEncode("<script>alert(1)</script>");
        var model = new Dictionary<string, object?>
        {
            ["AbogadoNombre"] = escaped,
            ["processes"] = new List<Dictionary<string, object?>>(),
            ["remaining"] = 0,
            ["UrlDashboard"] = "https://app.litigapp.co/novelties",
            ["Año"] = 2026,
        };

        var html = _sut.Render(EmailTemplate.UserDigest, model);

        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&lt;script&gt;", html);
    }
}
