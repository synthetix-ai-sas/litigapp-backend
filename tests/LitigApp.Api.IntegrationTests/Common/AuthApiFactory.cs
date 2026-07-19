using LitigApp.Application.Common.Abstractions;
using LitigApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace LitigApp.Api.IntegrationTests.Common;

/// <summary>
/// Factory for auth endpoint integration tests. Uses real JWT validation
/// (no TestAuthHandler override) with a fixed test secret.
/// </summary>
public sealed class AuthApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string TestJwtSecret = "test-secret-that-is-at-least-32-characters-long!";
    public const string TestJwtIssuer = "TestIssuer";
    public const string TestJwtAudience = "TestAudience";
    public const string TestFrontendBaseUrl = "https://test.litigapp.co";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = _postgres.GetConnectionString(),
                ["Jwt:Secret"] = TestJwtSecret,
                ["Jwt:Issuer"] = TestJwtIssuer,
                ["Jwt:Audience"] = TestJwtAudience,
                ["Jwt:AccessTokenMinutes"] = "15",
                ["Jwt:RefreshTokenDays"] = "7",
                ["Legal:TermsVersion"] = "v1.0",
                ["Legal:PrivacyVersion"] = "v1.0",
                ["Legal:DataProtectionEmail"] = "test@example.com",
                ["Auth:FrontendBaseUrl"] = TestFrontendBaseUrl,
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace real Resend sender with one that captures emails in-memory.
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<CapturingEmailSender>();
            services.AddSingleton<IEmailSender>(sp => sp.GetRequiredService<CapturingEmailSender>());
        });
    }

    /// <summary>Access captured emails to extract reset URLs in tests.</summary>
    public CapturingEmailSender EmailSender =>
        Services.GetRequiredService<CapturingEmailSender>();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", _postgres.GetConnectionString());

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }
}
