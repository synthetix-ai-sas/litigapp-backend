using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LitigApp.Api.IntegrationTests.Common;
using LitigApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LitigApp.Api.IntegrationTests.Features.Processes;

public sealed class ProcessWriteEndpointsTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public ProcessWriteEndpointsTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private void SetAuthHeader() =>
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ApiFactory.GenerateTestToken());

    private static JsonElement Root(string json) => JsonSerializer.Deserialize<JsonElement>(json, JsonOpts);

    private async Task<Guid> GetManizalesCourtIdAsync()
    {
        var json = await (await _client.GetAsync("/api/v1/catalog/cities/17001/courts")).Content.ReadAsStringAsync();
        var courts = JsonSerializer.Deserialize<JsonElement[]>(json, JsonOpts)!;
        return courts.First(c => c.GetProperty("officialCode").GetString() == "170014003010")
            .GetProperty("id").GetGuid();
    }

    // ── POST /full-number ────────────────────────────────────────────────

    [Fact]
    public async Task CreateFullNumber_WithoutAuth_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/processes/full-number",
            new { fileNumber = "17001400301020240000900" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateFullNumber_Valid_Returns201WithOkStatus()
    {
        SetAuthHeader();
        var response = await _client.PostAsJsonAsync("/api/v1/processes/full-number",
            new { fileNumber = "17001400301020240000900", alias = "Cliente X" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var root = Root(await response.Content.ReadAsStringAsync());
        root.GetProperty("syncStatus").GetString().Should().Be("ok");
        root.GetProperty("isPrivate").GetBoolean().Should().BeFalse();
        root.GetProperty("canDownloadPdf").GetBoolean().Should().BeTrue();
        root.GetProperty("subjects").GetArrayLength().Should().Be(2);
        root.GetProperty("actions").GetArrayLength().Should().Be(1);
        root.GetProperty("court").GetProperty("cityName").GetString().Should().Be("Manizales");
    }

    [Fact]
    public async Task CreateFullNumber_NotFoundInRama_Returns422()
    {
        SetAuthHeader();
        var response = await _client.PostAsJsonAsync("/api/v1/processes/full-number",
            new { fileNumber = "00000000000000000000000" });
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CreateFullNumber_PostOverviewFailure_Returns201Partial()
    {
        SetAuthHeader();
        var response = await _client.PostAsJsonAsync("/api/v1/processes/full-number",
            new { fileNumber = "17001400301020247777700" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var root = Root(await response.Content.ReadAsStringAsync());
        root.GetProperty("syncStatus").GetString().Should().Be("partial");
        root.GetProperty("canDownloadPdf").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task CreateFullNumber_PrivateProcess_Persists201Ok_WithNoSubjectsOrActions()
    {
        SetAuthHeader();
        // "88888" triggers the fake's private overview (esPrivado=true); the fake's
        // detail/subjects/actions all 404 for it, so a status of "ok" (not "partial")
        // proves those 3 endpoints were never called.
        const string privateFileNumber = "17001400301088888000904";
        var response = await _client.PostAsJsonAsync("/api/v1/processes/full-number",
            new { fileNumber = privateFileNumber, alias = "Privado demo" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var root = Root(await response.Content.ReadAsStringAsync());
        root.GetProperty("syncStatus").GetString().Should().Be("ok");
        root.GetProperty("isPrivate").GetBoolean().Should().BeTrue();
        // Private processes expose no subjects/actions → PDF has nothing to render.
        root.GetProperty("canDownloadPdf").GetBoolean().Should().BeFalse();
        root.GetProperty("subjects").GetArrayLength().Should().Be(0);
        root.GetProperty("actions").GetArrayLength().Should().Be(0);

        // DB-level: persisted as private, idle, with ZERO subject/action rows.
        var id = root.GetProperty("id").GetGuid();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var process = await db.Processes.AsNoTracking().FirstAsync(p => p.Id == id);
        process.IsPrivate.Should().BeTrue();
        process.SyncStatus.Should().Be("ok");
        process.SyncPhase.Should().Be("idle");
        process.LastCourtActionAt.Should().BeNull();
        (await db.ProcessSubjects.CountAsync(s => s.ProcessId == id)).Should().Be(0);
        (await db.ProcessActions.CountAsync(a => a.ProcessId == id)).Should().Be(0);
    }

    [Fact]
    public async Task CreateFullNumber_InvalidFileNumber_Returns400()
    {
        SetAuthHeader();
        var response = await _client.PostAsJsonAsync("/api/v1/processes/full-number",
            new { fileNumber = "123" });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateFullNumber_Duplicate_Returns409()
    {
        SetAuthHeader();
        var body = new { fileNumber = "17001400301020240000901" };
        (await _client.PostAsJsonAsync("/api/v1/processes/full-number", body))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await _client.PostAsJsonAsync("/api/v1/processes/full-number", body);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ── POST /wizard ─────────────────────────────────────────────────────

    [Fact]
    public async Task CreateWizard_Valid_Returns201()
    {
        SetAuthHeader();
        var courtId = await GetManizalesCourtIdAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/processes/wizard",
            new { cityId = "17001", courtId, filingYear = 2024, consecutive = "500" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var root = Root(await response.Content.ReadAsStringAsync());
        // composed: 170014003010 + 2024 + 5000000
        root.GetProperty("fileNumber").GetString().Should().Be("17001400301020245000000");
    }

    [Fact]
    public async Task CreateWizard_CourtCityMismatch_Returns400()
    {
        SetAuthHeader();
        var courtId = await GetManizalesCourtIdAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/processes/wizard",
            new { cityId = "66001", courtId, filingYear = 2024, consecutive = "501" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── POST /{id}/mark-attended & DELETE /{id} ──────────────────────────

    [Fact]
    public async Task MarkAttended_UnknownId_Returns404()
    {
        SetAuthHeader();
        var response = await _client.PostAsync($"/api/v1/processes/{Guid.NewGuid()}/mark-attended", null);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MarkAttended_OwnedProcess_Returns204()
    {
        SetAuthHeader();
        var created = Root(await (await _client.PostAsJsonAsync("/api/v1/processes/full-number",
            new { fileNumber = "17001400301020240000902" })).Content.ReadAsStringAsync());
        var id = created.GetProperty("id").GetGuid();

        var response = await _client.PostAsync($"/api/v1/processes/{id}/mark-attended", null);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task SoftDelete_UnknownId_Returns404()
    {
        SetAuthHeader();
        var response = await _client.DeleteAsync($"/api/v1/processes/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SoftDelete_OwnedProcess_Returns204AndThenNotFoundOnDetail()
    {
        SetAuthHeader();
        var created = Root(await (await _client.PostAsJsonAsync("/api/v1/processes/full-number",
            new { fileNumber = "17001400301020240000903" })).Content.ReadAsStringAsync());
        var id = created.GetProperty("id").GetGuid();

        (await _client.DeleteAsync($"/api/v1/processes/{id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await _client.GetAsync($"/api/v1/processes/{id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
