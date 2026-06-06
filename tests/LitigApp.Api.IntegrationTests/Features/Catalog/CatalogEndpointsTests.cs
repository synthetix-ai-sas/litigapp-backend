using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using LitigApp.Api.IntegrationTests.Common;

namespace LitigApp.Api.IntegrationTests.Features.Catalog;

public sealed class CatalogEndpointsTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;
    private readonly ApiFactory _factory;

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public CatalogEndpointsTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private void SetAuthHeader()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _factory.GenerateTestToken());
    }

    // ── GET /api/v1/catalog/departments ──────────────────────────────────

    [Fact]
    public async Task GetDepartments_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/catalog/departments");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDepartments_WithAuth_Returns200AndData()
    {
        SetAuthHeader();
        var response = await _client.GetAsync("/api/v1/catalog/departments");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonSerializer.Deserialize<JsonElement>(json, JsonOpts);
        var data = doc.GetProperty("data");
        data.GetArrayLength().Should().Be(3);
        data.EnumerateArray().Should().Contain(e => e.GetProperty("name").GetString() == "Caldas");
    }

    // ── GET /api/v1/catalog/departments/{id}/cities ──────────────────────

    [Fact]
    public async Task GetCitiesByDepartment_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/catalog/departments/17/cities");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCitiesByDepartment_KnownDept_Returns200WithCities()
    {
        SetAuthHeader();
        var response = await _client.GetAsync("/api/v1/catalog/departments/17/cities");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonSerializer.Deserialize<JsonElement>(json, JsonOpts);
        var data = doc.GetProperty("data");
        data.GetArrayLength().Should().Be(2);
        data.EnumerateArray().Should().Contain(e => e.GetProperty("name").GetString() == "Manizales");
    }

    [Fact]
    public async Task GetCitiesByDepartment_UnknownDept_Returns200WithEmptyList()
    {
        SetAuthHeader();
        var response = await _client.GetAsync("/api/v1/catalog/departments/99/cities");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonSerializer.Deserialize<JsonElement>(json, JsonOpts);
        doc.GetProperty("data").GetArrayLength().Should().Be(0);
    }

    // ── GET /api/v1/catalog/specialties ─────────────────────────────────

    [Fact]
    public async Task GetSpecialties_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/catalog/specialties");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSpecialties_WithAuth_Returns200AndData()
    {
        SetAuthHeader();
        var response = await _client.GetAsync("/api/v1/catalog/specialties");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonSerializer.Deserialize<JsonElement>(json, JsonOpts);
        doc.GetProperty("data").GetArrayLength().Should().Be(2);
    }

    // ── GET /api/v1/catalog/entities ────────────────────────────────────

    [Fact]
    public async Task GetEntities_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/catalog/entities");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetEntities_WithAuth_Returns200AndData()
    {
        SetAuthHeader();
        var response = await _client.GetAsync("/api/v1/catalog/entities");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonSerializer.Deserialize<JsonElement>(json, JsonOpts);
        doc.GetProperty("data").GetArrayLength().Should().Be(2);
    }

    // ── GET /api/v1/catalog/cities/{cityId}/courts ───────────────────────

    [Fact]
    public async Task GetCourtsByCity_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/catalog/cities/17001/courts");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCourtsByCity_Returns200WithActiveOnly()
    {
        SetAuthHeader();
        var response = await _client.GetAsync("/api/v1/catalog/cities/66001/courts");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonSerializer.Deserialize<JsonElement>(json, JsonOpts);
        // Pereira has 4 courts but 1 is inactive → 3 civil + 1 laboral = 4 active
        var data = doc.GetProperty("data");
        data.GetArrayLength().Should().Be(4);
        data.EnumerateArray().Should().NotContain(e =>
            e.GetProperty("name").GetString()!.Contains("004") &&
            e.GetProperty("name").GetString()!.Contains("PEREIRA"));
    }

    [Fact]
    public async Task GetCourtsByCity_WithSpecialtyFilter_ReturnsFilteredCourts()
    {
        SetAuthHeader();
        var response = await _client.GetAsync("/api/v1/catalog/cities/17001/courts?specialtyCode=03");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonSerializer.Deserialize<JsonElement>(json, JsonOpts);
        var data = doc.GetProperty("data");
        data.GetArrayLength().Should().Be(2); // only civil courts in Manizales
    }

    [Fact]
    public async Task GetCourtsByCity_UnknownCity_Returns200WithEmptyList()
    {
        SetAuthHeader();
        var response = await _client.GetAsync("/api/v1/catalog/cities/99999/courts");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonSerializer.Deserialize<JsonElement>(json, JsonOpts);
        doc.GetProperty("data").GetArrayLength().Should().Be(0);
    }

    // ── GET /api/v1/catalog/courts/search ────────────────────────────────

    [Fact]
    public async Task SearchCourts_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/catalog/courts/search?name=civil");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SearchCourts_WithoutNameParam_Returns400()
    {
        SetAuthHeader();
        var response = await _client.GetAsync("/api/v1/catalog/courts/search");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SearchCourts_ByName_Returns200WithMatchingCourts()
    {
        SetAuthHeader();
        var response = await _client.GetAsync("/api/v1/catalog/courts/search?name=civil+municipal");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonSerializer.Deserialize<JsonElement>(json, JsonOpts);
        // All civil municipal courts across all cities
        doc.GetProperty("data").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SearchCourts_WithCityIdFilter_ReturnsOnlyCityCourts()
    {
        SetAuthHeader();
        var response = await _client.GetAsync("/api/v1/catalog/courts/search?name=civil&cityId=17001");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonSerializer.Deserialize<JsonElement>(json, JsonOpts);
        var data = doc.GetProperty("data");
        data.EnumerateArray().Should().OnlyContain(e =>
            e.GetProperty("name").GetString()!.Contains("MANIZALES"));
    }
}
