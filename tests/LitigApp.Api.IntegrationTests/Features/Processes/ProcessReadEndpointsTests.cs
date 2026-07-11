using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using LitigApp.Api.IntegrationTests.Common;

namespace LitigApp.Api.IntegrationTests.Features.Processes;

public sealed class ProcessReadEndpointsTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public ProcessReadEndpointsTests(ApiFactory factory) => _client = factory.CreateClient();

    private void SetAuthHeader() =>
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ApiFactory.GenerateTestToken());

    private static JsonElement Root(string json) =>
        JsonSerializer.Deserialize<JsonElement>(json, JsonOpts);

    // ── GET /api/v1/processes/novelties ──────────────────────────────────

    [Fact]
    public async Task GetNovelties_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/processes/novelties");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetNovelties_WithAuth_ReturnsOnlyOwnedActiveUnattended()
    {
        SetAuthHeader();
        var response = await _client.GetAsync("/api/v1/processes/novelties");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var root = Root(await response.Content.ReadAsStringAsync());
        root.GetProperty("total").GetInt32().Should().Be(2);
        var items = root.GetProperty("items");
        items.GetArrayLength().Should().Be(2);
        // soft-deleted (...004), attended (...003) and other-user (...005) excluded
        foreach (var item in items.EnumerateArray())
            item.GetProperty("attended").GetBoolean().Should().BeFalse();
    }

    // ── GET /api/v1/processes ────────────────────────────────────────────

    [Fact]
    public async Task GetProcesses_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/processes");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProcesses_WithAuth_ReturnsOwnedActiveOnly()
    {
        SetAuthHeader();
        var response = await _client.GetAsync("/api/v1/processes");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var root = Root(await response.Content.ReadAsStringAsync());
        // p1, p2 (unattended) + p3 (attended) = 3; soft-deleted and other-user excluded
        root.GetProperty("total").GetInt32().Should().Be(3);
        // List rows expose isPrivate so the UI can show the "Privado" tag without opening detail.
        foreach (var item in root.GetProperty("items").EnumerateArray())
            item.GetProperty("isPrivate").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task GetProcesses_FilterByAttendedTrue_ReturnsOnlyAttended()
    {
        SetAuthHeader();
        var response = await _client.GetAsync("/api/v1/processes?attended=true");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var root = Root(await response.Content.ReadAsStringAsync());
        root.GetProperty("total").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task GetProcesses_FilterByFileNumberPrefix_ReturnsMatch()
    {
        SetAuthHeader();
        var response = await _client.GetAsync("/api/v1/processes?fileNumber=17001400301020240000002");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var root = Root(await response.Content.ReadAsStringAsync());
        root.GetProperty("total").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task GetProcesses_FilterByCourtName_ReturnsManizalesCourts()
    {
        SetAuthHeader();
        var response = await _client.GetAsync("/api/v1/processes?courtName=manizales");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var root = Root(await response.Content.ReadAsStringAsync());
        root.GetProperty("total").GetInt32().Should().Be(3);
    }

    // ── GET /api/v1/processes/{id} ───────────────────────────────────────

    [Fact]
    public async Task GetProcessById_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync($"/api/v1/processes/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProcessById_UnknownId_Returns404()
    {
        SetAuthHeader();
        var response = await _client.GetAsync($"/api/v1/processes/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetProcessById_OwnedProcess_ReturnsFullDetail()
    {
        SetAuthHeader();

        // discover the id of the "...001" process via the list
        var listRoot = Root(await (await _client.GetAsync("/api/v1/processes")).Content.ReadAsStringAsync());
        var target = listRoot.GetProperty("items").EnumerateArray()
            .First(i => i.GetProperty("fileNumber").GetString()!.StartsWith("17001400301020240000001"));
        var id = target.GetProperty("id").GetGuid();

        var response = await _client.GetAsync($"/api/v1/processes/{id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var root = Root(await response.Content.ReadAsStringAsync());
        root.GetProperty("syncStatus").GetString().Should().Be("ok");
        root.GetProperty("canDownloadPdf").GetBoolean().Should().BeTrue();
        root.GetProperty("court").GetProperty("cityName").GetString().Should().Be("Manizales");
        root.GetProperty("court").GetProperty("departmentName").GetString().Should().Be("Caldas");
        root.GetProperty("subjects").GetArrayLength().Should().Be(2);
        root.GetProperty("actions").GetArrayLength().Should().Be(1);
    }
}
