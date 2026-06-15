using System.Text;
using Hangfire;
using LitigApp.Api.Auth;
using LitigApp.Api.Features.Auth;
using LitigApp.Api.Features.Catalog;
using LitigApp.Api.Hangfire;
using LitigApp.Api.OpenApi;
using LitigApp.Application;
using LitigApp.Infrastructure;
using LitigApp.Infrastructure.Identity;
using LitigApp.Infrastructure.Persistence;
using LitigApp.Jobs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting LitigApp API");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, services, cfg) => cfg
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddApplication();
    builder.Services.AddJobs(builder.Configuration);

    // AddIdentity() overrides DefaultChallengeScheme to cookies (302 redirect).
    // Explicitly set all three so unauthenticated JWT requests get 401, not a redirect.
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultForbidScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer();

    // Configure via IOptions<JwtOptions> (post-Build) so WebApplicationFactory test
    // overrides are picked up. Reading builder.Configuration here would miss them.
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

    if (args.Contains("seed-catalog"))
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await CatalogSeeder.SeedAsync(db);
        return 0;
    }

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = [new HangfireDashboardAuthFilter()],
        DarkModeEnabled = false,
    });

    app.UseSerilogRequestLogging(opts =>
    {
        opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} → {StatusCode} in {Elapsed:0.0000} ms";
        opts.GetLevel = (httpCtx, elapsed, ex) => ex != null
            ? LogEventLevel.Error
            : httpCtx.Response.StatusCode > 499
                ? LogEventLevel.Error
                : elapsed > 1000 ? LogEventLevel.Warning : LogEventLevel.Information;
    });

    app.MapOpenApi();

    app.MapScalarApiReference(options =>
    {
        options.Title = "LitigApp API";
        options.OpenApiRoutePattern = "/openapi/v1.json";
    });

    app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
        .WithTags("Health")
        .WithName("HealthCheck")
        .WithSummary("Health check para Railway");

    app.MapAuthEndpoints();
    app.MapCatalogEndpoints();

    app.Run();
    return 0;
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

public partial class Program { }
