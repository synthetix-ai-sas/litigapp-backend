using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LitigApp.Api.Features.Auth;
using LitigApp.Api.IntegrationTests.Common;
using LitigApp.Application.Features.Auth;

namespace LitigApp.Api.IntegrationTests.Features.Auth;

public sealed class AuthEndpointsTests : IClassFixture<AuthApiFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointsTests(AuthApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidData_Returns201WithTokens()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = "newuser@example.com",
            password = "Password123",
            fullName = "Juan Pérez"
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
            fullName = "Test User"
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
            fullName = "Test User"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_WithValidCredentials_Returns200WithTokens()
    {
        await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = "loginuser@example.com",
            password = "Password123",
            fullName = "Login User"
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
            fullName = "Wrong Pass User"
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
            fullName = "Full Flow User"
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
