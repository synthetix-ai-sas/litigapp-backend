using LitigApp.Application.Features.Imports;

namespace LitigApp.Application.UnitTests.Features.Imports;

public class ImportPreviewProjectionTests
{
    private static ExcelPreview PreviewWith(params IReadOnlyDictionary<string, string?>[] rows) =>
        new("file.xlsx",
            [new ExcelColumn("A", "Radicado"), new ExcelColumn("B", "Notas")],
            rows);

    [Fact]
    public void Projects_mapped_radicado_and_notes_in_order()
    {
        var preview = PreviewWith(
            new Dictionary<string, string?> { ["A"] = "11001", ["B"] = "cliente 1" },
            new Dictionary<string, string?> { ["A"] = "05001", ["B"] = "cliente 2" });

        var rows = ImportPreviewProjection.Project(preview, radicadoCol: "A", notesCol: "B");

        Assert.Equal(2, rows.Count);
        Assert.Equal("11001", rows[0].Radicado);
        Assert.Equal("cliente 1", rows[0].Notes);
        Assert.Equal("05001", rows[1].Radicado);
        Assert.Equal("cliente 2", rows[1].Notes);
    }

    [Fact]
    public void Notes_is_null_when_notesCol_not_mapped()
    {
        var preview = PreviewWith(
            new Dictionary<string, string?> { ["A"] = "11001", ["B"] = "ignorado" });

        var rows = ImportPreviewProjection.Project(preview, radicadoCol: "A", notesCol: null);

        Assert.Equal("11001", rows[0].Radicado);
        Assert.Null(rows[0].Notes);
    }

    [Fact]
    public void Radicado_is_null_when_mapped_column_missing_but_row_kept()
    {
        var preview = PreviewWith(
            new Dictionary<string, string?> { ["B"] = "solo notas" });

        var rows = ImportPreviewProjection.Project(preview, radicadoCol: "A", notesCol: "B");

        Assert.Single(rows);
        Assert.Null(rows[0].Radicado);
        Assert.Equal("solo notas", rows[0].Notes);
    }
}
