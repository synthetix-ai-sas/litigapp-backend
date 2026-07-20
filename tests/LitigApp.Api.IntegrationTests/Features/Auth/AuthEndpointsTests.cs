using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
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

    // ──────────────────────────────────────────────────────────────────────────
    // Password-reset tests
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PasswordReset_WithNonExistentEmail_Returns200()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/password-reset/request",
            new { email = "ghost_nobody@example.com" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PasswordResetRequestedResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        body!.Message.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task PasswordReset_FullFlow_ChangesPassword()
    {
        const string email = "pr_fullflow@example.com";
        const string oldPassword = "Password123";
        const string newPassword = "NewPassword456";

        await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email, password = oldPassword, fullName = "PR Full Flow",
            acceptedTerms = true, acceptedPrivacy = true
        });

        var requestResp = await _client.PostAsJsonAsync(
            "/api/v1/auth/password-reset/request", new { email });
        requestResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var sentEmail = _factory.EmailSender.SentEmails
            .Where(e => e.To == email)
            .Should().ContainSingle().Subject;

        var (uid, token) = ExtractResetParams(sentEmail.HtmlBody);

        var confirmResp = await _client.PostAsJsonAsync(
            "/api/v1/auth/password-reset/confirm",
            new { uid, token, newPassword });
        confirmResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // New password works
        var loginNew = await _client.PostAsJsonAsync(
            "/api/v1/auth/login", new { email, password = newPassword });
        loginNew.StatusCode.Should().Be(HttpStatusCode.OK);

        // Old password is rejected
        var loginOld = await _client.PostAsJsonAsync(
            "/api/v1/auth/login", new { email, password = oldPassword });
        loginOld.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PasswordReset_WithInvalidToken_Returns400()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/password-reset/confirm",
            new
            {
                uid = "00000000-0000-0000-0000-000000000000",
                token = "this-is-not-a-valid-identity-token",
                newPassword = "NewPassword123"
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PasswordReset_RevokesRefreshTokensAfterReset()
    {
        const string email = "pr_revoke@example.com";
        const string oldPassword = "Password123";
        const string newPassword = "NewPassword456";

        await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email, password = oldPassword, fullName = "PR Revoke User",
            acceptedTerms = true, acceptedPrivacy = true
        });

        var loginResp = await _client.PostAsJsonAsync(
            "/api/v1/auth/login", new { email, password = oldPassword });
        var oldTokens = await loginResp.Content.ReadFromJsonAsync<AuthTokensResponse>();

        await _client.PostAsJsonAsync(
            "/api/v1/auth/password-reset/request", new { email });

        var sentEmail = _factory.EmailSender.SentEmails
            .Where(e => e.To == email)
            .Should().ContainSingle().Subject;

        var (uid, token) = ExtractResetParams(sentEmail.HtmlBody);

        var confirmResp = await _client.PostAsJsonAsync(
            "/api/v1/auth/password-reset/confirm",
            new { uid, token, newPassword });
        confirmResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Old refresh token must be revoked after the password reset
        var refreshResp = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            accessToken = oldTokens!.AccessToken,
            refreshToken = oldTokens.RefreshToken
        });
        refreshResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // Extracts uid and URL-decoded token from the reset link embedded in the HTML email body.
    private static (string Uid, string Token) ExtractResetParams(string htmlBody)
    {
        var match = Regex.Match(
            htmlBody,
            @"href=""(https://test\.litigapp\.co/reset-password[^""]+)""",
            RegexOptions.IgnoreCase);

        match.Success.Should().BeTrue("the email should contain the reset href");

        var uri = new Uri(match.Groups[1].Value);
        var parts = uri.Query.TrimStart('?')
            .Split('&')
            .Select(p => p.Split('=', 2))
            .ToDictionary(p => p[0], p => Uri.UnescapeDataString(p[1]));

        return (parts["uid"], parts["token"]);
    }
}
