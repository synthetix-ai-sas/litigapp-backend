using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using LitigApp.Api.IntegrationTests.Common;

namespace LitigApp.Api.IntegrationTests.Features.Processes;

public sealed class ProcessPdfEndpointTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public ProcessPdfEndpointTests(ApiFactory factory) => _client = factory.CreateClient();

    private void SetAuthHeader() =>
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ApiFactory.GenerateTestToken());

    private async Task<Guid> FindProcessIdByFilePrefixAsync(string prefix)
    {
        var json = await (await _client.GetAsync("/api/v1/processes")).Content.ReadAsStringAsync();
        var root = JsonSerializer.Deserialize<JsonElement>(json, JsonOpts);
        return root.GetProperty("items").EnumerateArray()
            .First(i => i.GetProperty("fileNumber").GetString()!.StartsWith(prefix))
            .GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task GetPdf_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync($"/api/v1/processes/{Guid.NewGuid()}/pdf");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPdf_UnknownId_Returns404()
    {
        SetAuthHeader();
        var response = await _client.GetAsync($"/api/v1/processes/{Guid.NewGuid()}/pdf");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPdf_ProcessNotSynced_Returns409()
    {
        SetAuthHeader();
        // seeded process ...003 has sync_status = "partial"
        var id = await FindProcessIdByFilePrefixAsync("17001400301020240000003");

        var response = await _client.GetAsync($"/api/v1/processes/{id}/pdf");
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetPdf_SyncedProcess_Returns200WithPdfBytes()
    {
        SetAuthHeader();
        // seeded process ...001 has sync_status = "ok" + subjects + actions
        var id = await FindProcessIdByFilePrefixAsync("17001400301020240000001");

        var response = await _client.GetAsync($"/api/v1/processes/{id}/pdf");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");

        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Length.Should().BeGreaterThan(0);
        Encoding.ASCII.GetString(bytes, 0, 4).Should().Be("%PDF");
    }
}
