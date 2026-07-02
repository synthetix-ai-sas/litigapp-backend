using ClosedXML.Excel;
using FluentAssertions;
using LitigApp.Infrastructure.Imports;

namespace LitigApp.Infrastructure.UnitTests.Imports;

public class ClosedXmlExcelParserTests
{
    private readonly ClosedXmlExcelParser _parser = new();

    [Fact]
    public void Parse_ReadsHeadersAsColumnsKeyedByLetter()
    {
        using var stream = Build(ws =>
        {
            ws.Cell(1, 1).Value = "Radicado";
            ws.Cell(1, 2).Value = "Cliente";
            ws.Cell(2, 1).Value = "11001310300120230001200";
            ws.Cell(2, 2).Value = "Acme";
        });

        var preview = _parser.Parse(stream, "p.xlsx");

        preview.FileName.Should().Be("p.xlsx");
        preview.Columns.Should().HaveCount(2);
        preview.Columns[0].Should().Be(new LitigApp.Application.Features.Imports.ExcelColumn("A", "Radicado"));
        preview.Columns[1].Key.Should().Be("B");
        preview.Rows.Should().HaveCount(1);
        preview.Rows[0]["A"].Should().Be("11001310300120230001200");
        preview.Rows[0]["B"].Should().Be("Acme");
    }

    [Fact]
    public void Parse_TrimsValues_AndMapsBlankCellsToNull()
    {
        using var stream = Build(ws =>
        {
            ws.Cell(1, 1).Value = "Radicado";
            ws.Cell(1, 2).Value = "Notas";
            ws.Cell(2, 1).Value = "  11001310300120230001200  ";
            // B2 left blank
        });

        var preview = _parser.Parse(stream, "p.xlsx");

        preview.Rows[0]["A"].Should().Be("11001310300120230001200");
        preview.Rows[0]["B"].Should().BeNull();
    }

    [Fact]
    public void Parse_WithHeaderOnly_ReturnsColumnsAndNoRows()
    {
        using var stream = Build(ws =>
        {
            ws.Cell(1, 1).Value = "Radicado";
        });

        var preview = _parser.Parse(stream, "p.xlsx");

        preview.Columns.Should().ContainSingle();
        preview.Rows.Should().BeEmpty();
    }

    [Fact]
    public void Parse_EmptyWorkbook_ReturnsNoColumnsOrRows()
    {
        using var stream = Build(_ => { });

        var preview = _parser.Parse(stream, "empty.xlsx");

        preview.Columns.Should().BeEmpty();
        preview.Rows.Should().BeEmpty();
    }

    [Fact]
    public void Parse_NonXlsxStream_Throws()
    {
        using var stream = new MemoryStream("not a spreadsheet"u8.ToArray());

        var act = () => _parser.Parse(stream, "x.xlsx");

        act.Should().Throw<Exception>();
    }

    private static MemoryStream Build(Action<IXLWorksheet> fill)
    {
        using var workbook = new XLWorkbook();
        fill(workbook.AddWorksheet("Hoja1"));
        var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }
}
