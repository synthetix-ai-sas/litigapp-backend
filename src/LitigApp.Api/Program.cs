using System.Text;
using LitigApp.Api.Auth;
using LitigApp.Api.Features.Auth;
using LitigApp.Api.Features.Catalog;
using LitigApp.Api.OpenApi;
using LitigApp.Application;
using LitigApp.Infrastructure;
using LitigApp.Infrastructure.Identity;
using LitigApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

// ── JWT Bearer auth ───────────────────────────────────────────────────────────
// AddIdentity() sets DefaultChallengeScheme to cookie (redirect). Override all three
// so unauthenticated requests to JWT-protected endpoints get 401, not a cookie redirect.
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultForbidScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer();

// Configure JWT Bearer via IOptions<JwtOptions> (resolved post-Build) so that
// WebApplicationFactory in-memory config overrides are picked up in tests.
// Reading builder.Configuration here (pre-Build) would miss ConfigureWebHost overrides.
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<Microsoft.Extensions.Options.IOptions<JwtOptions>>((bearerOpts, jwtOpts) =>
    {
        bearerOpts.MapInboundClaims = false;
        bearerOpts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero,
            ValidIssuer = jwtOpts.Value.Issuer,
            ValidAudience = jwtOpts.Value.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOpts.Value.Secret))
        };
    });

builder.Services.AddAuthorizationPolicies();

// ── OpenAPI (native .NET 10 — NO Swashbuckle) ─────────────────────────────────
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info = new()
        {
            Title = "LitigApp API",
            Version = "v1",
            Description = "Backend de LitigApp — monitoreo de procesos judiciales en Colombia."
        };
        return Task.CompletedTask;
    });
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

var app = builder.Build();

// ── CLI: dotnet run -- seed-catalog ──────────────────────────────────────────
if (args.Contains("seed-catalog"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await CatalogSeeder.SeedAsync(db);
    return;
}

app.UseAuthentication();
app.UseAuthorization();

// ── OpenAPI + Scalar UI ───────────────────────────────────────────────────────
// TODO: in a real production deployment consider restricting /scalar to Development
// or behind auth. For now enabled in all environments so any dev can explore the API.
app.MapOpenApi();

app.MapScalarApiReference(options =>
{
    options.Title = "LitigApp API";
    options.OpenApiRoutePattern = "/openapi/v1.json";
});

// ── Endpoints ─────────────────────────────────────────────────────────────────
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .WithTags("Health")
    .WithName("HealthCheck")
    .WithSummary("Health check para Railway");

app.MapAuthEndpoints();
app.MapCatalogEndpoints();

app.Run();

public partial class Program { }
