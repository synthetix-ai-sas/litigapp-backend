using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using ClosedXML.Excel;
using FluentAssertions;
using LitigApp.Api.IntegrationTests.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace LitigApp.Api.IntegrationTests.Features.Imports;

public sealed class ImportPreviewEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private HttpClient AuthedClient(Action<IWebHostBuilder>? configure = null)
    {
        var f = configure is null ? factory : factory.WithWebHostBuilder(configure);
        var client = f.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ApiFactory.GenerateTestToken());
        return client;
    }

    [Fact]
    public async Task Preview_WithValidXlsx_ReturnsColumnsRowsAndPreviewId()
    {
        var bytes = BuildXlsx(dataRows: 3, "Radicado", "Cliente", "Notas");
        using var content = Multipart(bytes, "portafolio.xlsx");

        var response = await AuthedClient().PostAsync("/api/v1/imports/preview", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PreviewDto>();
        body.Should().NotBeNull();
        body!.PreviewId.Should().NotBeEmpty();
        body.TotalRows.Should().Be(3);
        body.Columns.Should().HaveCount(3);
        body.Columns[0].Key.Should().Be("A");
        body.Columns[0].Header.Should().Be("Radicado");
    }

    [Fact]
    public async Task Preview_WithTooManyRows_Returns422TooManyRows()
    {
        var client = AuthedClient(b => b.ConfigureAppConfiguration((_, c) =>
            c.AddInMemoryCollection(new Dictionary<string, string?> { ["Import:MaxRows"] = "1" })));
        using var content = Multipart(BuildXlsx(dataRows: 2, "Radicado"), "big.xlsx");

        var response = await client.PostAsync("/api/v1/imports/preview", content);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await ReadCode(response)).Should().Be("TOO_MANY_ROWS");
    }

    [Fact]
    public async Task Preview_WithOversizedFile_Returns413()
    {
        var client = AuthedClient(b => b.ConfigureAppConfiguration((_, c) =>
            c.AddInMemoryCollection(new Dictionary<string, string?> { ["Import:MaxFileSizeBytes"] = "1024" })));
        using var content = Multipart(BuildXlsx(dataRows: 3, "Radicado", "Cliente"), "portafolio.xlsx");

        var response = await client.PostAsync("/api/v1/imports/preview", content);

        response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
        (await ReadCode(response)).Should().Be("FILE_TOO_LARGE");
    }

    [Fact]
    public async Task Preview_WithNonXlsxBytes_Returns422InvalidFile()
    {
        using var content = Multipart(Encoding.UTF8.GetBytes("this is not a spreadsheet"), "notes.xlsx");

        var response = await AuthedClient().PostAsync("/api/v1/imports/preview", content);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await ReadCode(response)).Should().Be("INVALID_FILE");
    }

    [Fact]
    public async Task Preview_WithoutAuth_Returns401()
    {
        using var content = Multipart(BuildXlsx(dataRows: 1, "Radicado"), "x.xlsx");

        var response = await factory.CreateClient().PostAsync("/api/v1/imports/preview", content);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── helpers ────────────────────────────────────────────────────────────────

    private static MultipartFormDataContent Multipart(byte[] bytes, string fileName)
    {
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType =
            new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        return new MultipartFormDataContent { { fileContent, "file", fileName } };
    }

    private static byte[] BuildXlsx(int dataRows, params string[] headers)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet("Hoja1");
        for (var c = 0; c < headers.Length; c++)
            ws.Cell(1, c + 1).Value = headers[c];
        for (var r = 0; r < dataRows; r++)
            for (var c = 0; c < headers.Length; c++)
                ws.Cell(r + 2, c + 1).Value = $"r{r}c{c}";

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    private static async Task<string?> ReadCode(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        return problem is not null && problem.TryGetValue("code", out var code) ? code.ToString() : null;
    }

    private sealed record PreviewDto(Guid PreviewId, List<ColumnDto> Columns, List<Dictionary<string, string?>> Rows, int TotalRows);

    private sealed record ColumnDto(string Key, string? Header);
}
