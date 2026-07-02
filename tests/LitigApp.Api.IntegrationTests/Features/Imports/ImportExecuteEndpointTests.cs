using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ClosedXML.Excel;
using FluentAssertions;
using LitigApp.Api.IntegrationTests.Common;
using LitigApp.Domain.Imports;
using LitigApp.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace LitigApp.Api.IntegrationTests.Features.Imports;

/// <summary>
/// Tests that mutate shared DB state (seed Running jobs) each get their own ApiFactory
/// so they cannot contaminate the clean-state tests.
/// </summary>
public sealed class ImportExecuteEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private HttpClient AuthedClient() => Authed(factory.CreateClient());
    private static HttpClient Authed(HttpClient c)
    {
        c.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ApiFactory.GenerateTestToken());
        return c;
    }

    // ── clean-state tests (use shared factory) ────────────────────────────────

    [Fact]
    public async Task Execute_WithValidPreviewId_Returns202AndJobId()
    {
        var client = AuthedClient();

        using var previewContent = Multipart(BuildXlsx(1, "Radicado"), "p.xlsx");
        var previewResp = await client.PostAsync("/api/v1/imports/preview", previewContent);
        previewResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var preview = await previewResp.Content.ReadFromJsonAsync<PreviewDto>();

        var executeResp = await client.PostAsJsonAsync("/api/v1/imports", new
        {
            previewId = preview!.PreviewId,
            mapping   = new { radicadoCol = "A", notesCol = (string?)null },
            fileName  = "portafolio.xlsx",
        });

        executeResp.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var body = await executeResp.Content.ReadFromJsonAsync<ExecuteDto>();
        body!.ImportJobId.Should().NotBeEmpty();
        body.Status.Should().Be(ImportStatus.Pending);
    }

    [Fact]
    public async Task Execute_WithExpiredPreviewId_Returns404PreviewExpired()
    {
        var executeResp = await AuthedClient().PostAsJsonAsync("/api/v1/imports", new
        {
            previewId = Guid.NewGuid(),
            mapping   = new { radicadoCol = "A", notesCol = (string?)null },
        });

        executeResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ReadCode(executeResp)).Should().Be("PREVIEW_EXPIRED");
    }

    [Fact]
    public async Task GetActive_WhenNoActiveJob_Returns204()
    {
        var resp = await AuthedClient().GetAsync("/api/v1/imports/active");
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ── tests that seed DB state — each gets its own ApiFactory ──────────────

    [Fact]
    public async Task Execute_WithActiveImport_Returns409ImportInProgress()
    {
        await using var f = new ApiFactory();
        await f.InitializeAsync();
        var client = Authed(f.CreateClient());

        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.ImportJobs.Add(new ImportJob
        {
            Id        = Guid.NewGuid(),
            UserId    = TestAuthHandler.TestUserId,
            FileName  = "old.xlsx",
            Status    = ImportStatus.Running,
            PreviewId = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
        });
        await db.SaveChangesAsync();

        using var previewContent = Multipart(BuildXlsx(1, "Radicado"), "p.xlsx");
        var previewResp = await client.PostAsync("/api/v1/imports/preview", previewContent);
        var preview = await previewResp.Content.ReadFromJsonAsync<PreviewDto>();

        var executeResp = await client.PostAsJsonAsync("/api/v1/imports", new
        {
            previewId = preview!.PreviewId,
            mapping   = new { radicadoCol = "A", notesCol = (string?)null },
        });

        executeResp.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadCode(executeResp)).Should().Be("IMPORT_IN_PROGRESS");
    }

    [Fact]
    public async Task GetActive_WhenJobActive_ReturnsJob()
    {
        await using var f = new ApiFactory();
        await f.InitializeAsync();
        var client = Authed(f.CreateClient());

        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.ImportJobs.Add(new ImportJob
        {
            Id        = Guid.NewGuid(),
            UserId    = TestAuthHandler.TestUserId,
            FileName  = "active.xlsx",
            TotalRows = 50,
            Status    = ImportStatus.Running,
            PreviewId = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
        });
        await db.SaveChangesAsync();

        var resp = await client.GetAsync("/api/v1/imports/active");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<ImportJobDto>();
        body!.Status.Should().Be(ImportStatus.Running);
        body.TotalRows.Should().Be(50);
    }

    // ── helpers ────────────────────────────────────────────────────────────────

    private static MultipartFormDataContent Multipart(byte[] bytes, string fileName)
    {
        var c = new ByteArrayContent(bytes);
        c.Headers.ContentType = new MediaTypeHeaderValue(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        return new MultipartFormDataContent { { c, "file", fileName } };
    }

    private static byte[] BuildXlsx(int dataRows, params string[] headers)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Hoja1");
        for (var c = 0; c < headers.Length; c++) ws.Cell(1, c + 1).Value = headers[c];
        for (var r = 0; r < dataRows; r++)
            for (var c = 0; c < headers.Length; c++)
                ws.Cell(r + 2, c + 1).Value = $"v{r}{c}";
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static async Task<string?> ReadCode(HttpResponseMessage resp)
    {
        var doc = await resp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        return doc?.TryGetValue("code", out var v) == true ? v.ToString() : null;
    }

    private sealed record PreviewDto(Guid PreviewId);
    private sealed record ExecuteDto(Guid ImportJobId, string Status);
    private sealed record ImportJobDto(Guid Id, string Status, int TotalRows);
}
