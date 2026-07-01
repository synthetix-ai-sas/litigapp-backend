using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LitigApp.Api.Features.Auth;
using LitigApp.Api.IntegrationTests.Common;
using LitigApp.Application.Features.Auth;
using LitigApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LitigApp.Api.IntegrationTests.Features.Auth;

public sealed class AuthEndpointsTests : IClassFixture<AuthApiFactory>
{
    private readonly HttpClient _client;
    private readonly AuthApiFactory _factory;

    public AuthEndpointsTests(AuthApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidData_Returns201WithTokens()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = "newuser@example.com",
            password = "Password123",
            fullName = "Juan Pérez",
            acceptedTerms = true,
            acceptedPrivacy = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<AuthTokensResponse>();
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.RefreshToken.Should().NotBeNullOrWhiteSpace();
        body.ExpiresInSeconds.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409()
    {
        var payload = new
        {
            email = "duplicate@example.com",
            password = "Password123",
            fullName = "Test User",
            acceptedTerms = true,
            acceptedPrivacy = true
        };
        await _client.PostAsJsonAsync("/api/v1/auth/register", payload);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_Returns409()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = "not-an-email",
            password = "Password123",
            fullName = "Test User",
            acceptedTerms = true,
            acceptedPrivacy = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WithoutAcceptedTerms_Returns422_AndDoesNotCreateUser()
    {
        const string email = "noterms@example.com";

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = "Password123",
            fullName = "No Terms User",
            acceptedTerms = false,
            acceptedPrivacy = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        // User must NOT have been created — login should fail
        var loginResp = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Password123" });
        loginResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_WithoutAcceptedPrivacy_Returns422_AndDoesNotCreateUser()
    {
        const string email = "noprivacy@example.com";

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = "Password123",
            fullName = "No Privacy User",
            acceptedTerms = true,
            acceptedPrivacy = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var loginResp = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Password123" });
        loginResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_WithBothLegalFalse_Returns422()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = "noboth@example.com",
            password = "Password123",
            fullName = "No Legal User",
            acceptedTerms = false,
            acceptedPrivacy = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Register_AcceptingLegal_Creates2LegalAcceptanceRowsWithCorrectVersions()
    {
        const string email = "legalcheck@example.com";

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = "Password123",
            fullName = "Legal Check User",
            acceptedTerms = true,
            acceptedPrivacy = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = await db.Users.FirstAsync(u => u.Email == email);
        var acceptances = await db.LegalAcceptances
            .Where(la => la.UserId == user.Id)
            .OrderBy(la => la.DocumentType)
            .ToListAsync();

        acceptances.Should().HaveCount(2);

        var privacy = acceptances.First(a => a.DocumentType == "privacy");
        privacy.DocumentVersion.Should().Be("v1.0");
        privacy.AcceptedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));

        var terms = acceptances.First(a => a.DocumentType == "terms");
        terms.DocumentVersion.Should().Be("v1.0");
        terms.AcceptedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Login_WithValidCredentials_Returns200WithTokens()
    {
        await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = "loginuser@example.com",
            password = "Password123",
            fullName = "Login User",
            acceptedTerms = true,
            acceptedPrivacy = true
        });

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "loginuser@example.com",
            password = "Password123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthTokensResponse>();
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = "wrongpass@example.com",
            password = "Password123",
            fullName = "Wrong Pass User",
            acceptedTerms = true,
            acceptedPrivacy = true
        });

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "wrongpass@example.com",
            password = "WrongPassword!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_Login_Me_FullFlow_ReturnsCorrectUserId()
    {
        const string email = "fullflow@example.com";
        const string password = "Password123";

        // Register
        var registerResp = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password,
            fullName = "Full Flow User",
            acceptedTerms = true,
            acceptedPrivacy = true
        });
        registerResp.StatusCode.Should().Be(HttpStatusCode.Created);

        // Login
        var loginResp = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        loginResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokens = await loginResp.Content.ReadFromJsonAsync<AuthTokensResponse>();
        tokens.Should().NotBeNull();

        // Access protected endpoint
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokens!.AccessToken);

        var meResp = await _client.GetAsync("/api/v1/auth/me");
        meResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var me = await meResp.Content.ReadFromJsonAsync<MeResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        me.Should().NotBeNull();
        me!.Email.Should().Be(email);
        me.UserId.Should().NotBeNullOrWhiteSpace();
    }
}
