using LitigApp.Application.Features.Notifications;

namespace LitigApp.Application.UnitTests.Features.Notifications;

public class ImportCompleteEmailModelBuilderTests
{
    [Fact]
    public void Build_WithErrors_SetsTieneErroresTrue()
    {
        var model = ImportCompleteEmailModelBuilder.Build(
            "Sergio Molina", "portafolio.xlsx",
            totalImported: 82, totalDuplicates: 3, totalErrors: 5,
            dashboardUrl: "https://app.litigapp.co/processes", year: 2026);

        Assert.Equal("Sergio Molina", model["AbogadoNombre"]);
        Assert.Equal("portafolio.xlsx", model["NombreArchivo"]);
        Assert.Equal(82, model["TotalImportados"]);
        Assert.Equal(3, model["TotalDuplicados"]);
        Assert.Equal(5, model["TotalErrores"]);
        Assert.Equal(true, model["TieneErrores"]);
        Assert.Equal(2026, model["Año"]);
    }

    [Fact]
    public void Build_NoErrors_SetsTieneErroresFalse()
    {
        var model = ImportCompleteEmailModelBuilder.Build(
            "Sergio", "x.xlsx", totalImported: 10, totalDuplicates: 0, totalErrors: 0,
            dashboardUrl: "https://app.litigapp.co/processes", year: 2026);

        Assert.Equal(false, model["TieneErrores"]);
    }

    [Fact]
    public void Build_EscapesFreeTextFields()
    {
        var model = ImportCompleteEmailModelBuilder.Build(
            "<script>alert(1)</script>", "<script>bad.xlsx</script>",
            totalImported: 1, totalDuplicates: 0, totalErrors: 0,
            dashboardUrl: "https://app.litigapp.co/processes", year: 2026);

        Assert.DoesNotContain("<script>", (string)model["AbogadoNombre"]!);
        Assert.DoesNotContain("<script>", (string)model["NombreArchivo"]!);
    }

    [Fact]
    public void BuildSubject_UsesSuccessCount()
    {
        Assert.Equal("Importación completada — 82 procesos cargados", ImportCompleteEmailModelBuilder.BuildSubject(82));
    }
}
