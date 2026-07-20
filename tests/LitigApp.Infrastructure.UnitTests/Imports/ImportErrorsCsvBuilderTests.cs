using System.Text;
using LitigApp.Application.Features.Notifications.Dtos;
using LitigApp.Infrastructure.Imports;

namespace LitigApp.Infrastructure.UnitTests.Imports;

public class ImportErrorsCsvBuilderTests
{
    private readonly ImportErrorsCsvBuilder _sut = new();

    [Fact]
    public void Build_StartsWithUtf8Bom()
    {
        var bytes = _sut.Build([]);

        var bom = Encoding.UTF8.GetPreamble();
        Assert.Equal(bom, bytes.Take(bom.Length).ToArray());
    }

    [Fact]
    public void Build_EmptyList_HeaderOnly()
    {
        var bytes = _sut.Build([]);

        var text = DecodeWithoutBom(bytes);
        var lines = text.TrimEnd('\r', '\n').Split("\r\n");
        Assert.Single(lines);
        Assert.Equal("Fila,Radicado,Motivo", lines[0]);
    }

    [Fact]
    public void Build_WritesHeaderAndDataRow()
    {
        var bytes = _sut.Build([new ImportErrorRow(5, "17001400301020240019200", "SOME_UNMAPPED_CODE", "Error importando el proceso.")]);

        var text = DecodeWithoutBom(bytes);
        var lines = text.TrimEnd('\r', '\n').Split("\r\n");
        Assert.Equal("Fila,Radicado,Motivo", lines[0]);
        Assert.Equal("5,17001400301020240019200,Error importando el proceso.", lines[1]);
    }

    [Fact]
    public void Build_KnownCode_UsesCanonicalSpanishMessage_IgnoresStoredMessage()
    {
        var bytes = _sut.Build([new ImportErrorRow(2, "123", "INVALID_RADICADO", "some raw stored text")]);

        var text = DecodeWithoutBom(bytes);
        Assert.Contains("Radicado inválido: no tiene 23 dígitos", text);
        Assert.DoesNotContain("some raw stored text", text);
    }

    [Fact]
    public void Build_ProcessNotFoundCode_MapsToSpanishMessage()
    {
        var bytes = _sut.Build([new ImportErrorRow(3, "123", "PROCESS_NOT_FOUND_IN_RAMA", "raw")]);

        Assert.Contains("No encontrado en la Rama Judicial", DecodeWithoutBom(bytes));
    }

    [Fact]
    public void Build_UnknownCode_FallsBackToStoredMessage()
    {
        var bytes = _sut.Build([new ImportErrorRow(4, "123", "TOTALLY_UNKNOWN_CODE", "mensaje original del sistema")]);

        Assert.Contains("mensaje original del sistema", DecodeWithoutBom(bytes));
    }

    [Fact]
    public void Build_CellStartingWithEquals_IsSanitizedAgainstCsvInjection()
    {
        var bytes = _sut.Build([new ImportErrorRow(1, "=SUM(A1:A9)", "TOTALLY_UNKNOWN_CODE", "x")]);

        var text = DecodeWithoutBom(bytes);
        Assert.DoesNotContain(",=SUM(A1:A9)", text);
        Assert.Contains("'=SUM(A1:A9)", text);
    }

    [Theory]
    [InlineData("+cmd")]
    [InlineData("-cmd")]
    [InlineData("@cmd")]
    public void Build_CellStartingWithDangerousPrefix_IsSanitized(string dangerous)
    {
        var bytes = _sut.Build([new ImportErrorRow(1, dangerous, "TOTALLY_UNKNOWN_CODE", "x")]);

        Assert.Contains($"'{dangerous}", DecodeWithoutBom(bytes));
    }

    [Fact]
    public void Build_MessageWithComma_IsQuotedPerRfc4180()
    {
        var bytes = _sut.Build([new ImportErrorRow(1, "123", "TOTALLY_UNKNOWN_CODE", "Error: fila, columna inválida")]);

        Assert.Contains("\"Error: fila, columna inválida\"", DecodeWithoutBom(bytes));
    }

    [Fact]
    public void Build_MessageWithQuote_IsEscapedAndQuoted()
    {
        var bytes = _sut.Build([new ImportErrorRow(1, "123", "TOTALLY_UNKNOWN_CODE", "dice \"error\" aquí")]);

        Assert.Contains("\"dice \"\"error\"\" aquí\"", DecodeWithoutBom(bytes));
    }

    private static string DecodeWithoutBom(byte[] bytes)
    {
        var bom = Encoding.UTF8.GetPreamble();
        var withoutBom = bytes.Skip(bom.Length).ToArray();
        return Encoding.UTF8.GetString(withoutBom);
    }
}
