using LitigApp.Application.Features.Notifications;
using LitigApp.Application.Features.Notifications.Dtos;

namespace LitigApp.Application.UnitTests.Features.Notifications;

public class UserDigestEmailModelBuilderTests
{
    private static readonly DateTimeOffset SomeDate = new(2026, 3, 20, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Build_MapsShownProcessesIntoLoopList_WithLowercaseKeys()
    {
        var cut = new DigestCut(
            [new ChangedProcessDto(Guid.NewGuid(), "17001400301020240019200", SomeDate, "Fijacion estado", "nota")],
            Remaining: 0, Total: 1);

        var model = UserDigestEmailModelBuilder.Build("Sergio Molina", cut, "https://app.litigapp.co/novelties", 2026);

        Assert.Equal("Sergio Molina", model["AbogadoNombre"]);
        Assert.Equal(0, model["remaining"]);
        Assert.Equal("https://app.litigapp.co/novelties", model["UrlDashboard"]);
        Assert.Equal(2026, model["Año"]);

        var processes = Assert.IsAssignableFrom<IEnumerable<Dictionary<string, object?>>>(model["processes"]).ToList();
        var row = Assert.Single(processes);
        Assert.Equal("17001400301020240019200", row["radicado"]);
        Assert.Equal("2026-03-20", row["fecha"]);
        Assert.Equal("Fijacion estado", row["accion"]);
        Assert.Equal("nota", row["anotacion"]);
    }

    [Fact]
    public void Build_EscapesFreeTextFields_ScriptTagNeverAppearsRaw()
    {
        var cut = new DigestCut(
            [new ChangedProcessDto(Guid.NewGuid(), "17001400301020240019200", SomeDate,
                "<script>alert(1)</script>", "<img src=x onerror=alert(2)>")],
            Remaining: 0, Total: 1);

        var model = UserDigestEmailModelBuilder.Build(
            "<script>alert(3)</script>", cut, "https://app.litigapp.co/novelties", 2026);

        Assert.DoesNotContain("<script>", (string)model["AbogadoNombre"]!);
        Assert.Contains("&lt;script&gt;", (string)model["AbogadoNombre"]!);

        var row = ((IEnumerable<Dictionary<string, object?>>)model["processes"]!).Single();
        Assert.DoesNotContain("<script>", (string)row["accion"]!);
        Assert.DoesNotContain("<img", (string)row["anotacion"]!);
    }

    [Fact]
    public void BuildSubject_SingleProcess_UsesSingular()
    {
        Assert.Equal("Tienes 1 novedad en tus procesos — LitigApp", UserDigestEmailModelBuilder.BuildSubject(1));
    }

    [Fact]
    public void BuildSubject_MultipleProcesses_UsesPluralWithTotal()
    {
        Assert.Equal("Tienes 7 novedades en tus procesos — LitigApp", UserDigestEmailModelBuilder.BuildSubject(7));
    }
}
