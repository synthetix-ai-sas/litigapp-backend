using System.Net;
using Hangfire;
using Hangfire.PostgreSql;
using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Imports;
using LitigApp.Infrastructure.Catalog;
using LitigApp.Infrastructure.Imports;
using LitigApp.Infrastructure.ExternalApis.RamaJudicial;
using LitigApp.Infrastructure.Identity;
using LitigApp.Infrastructure.Notifications.Email;
using LitigApp.Infrastructure.Persistence;
using LitigApp.Infrastructure.Pdf;
using LitigApp.Infrastructure.Persistence.Repositories;
using LitigApp.Infrastructure.Processes;
using LitigApp.Infrastructure.Sync;
using LitigApp.Infrastructure.Time;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace LitigApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        bool isWorker)
    {
        // ── Database ──────────────────────────────────────────────────────────
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

        services.AddDbContext<AppDbContext>(options =>
            options
                .UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention());

        services.AddScoped<IEmailSender, NoOpEmailSender>();

        // JWT/Identity — api role only; the worker never issues/validates tokens.
        if (!isWorker)
        {
            services.AddOptions<JwtOptions>()
                .BindConfiguration(JwtOptions.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddHttpContextAccessor();
            services.AddScoped<IJwtTokenService, JwtTokenService>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IIdentityService, IdentityService>();
            services.AddScoped<IAuthRepository, AuthRepository>();

            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
                {
                    options.Password.RequiredLength = 8;
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireNonAlphanumeric = false;
                    options.User.RequireUniqueEmail = true;
                    options.SignIn.RequireConfirmedEmail = false; // set to true in production
                })
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();
        }

        // ── Time ──────────────────────────────────────────────────────────────
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<ISyncDelay, RealSyncDelay>();

        // ── Rama Judicial HTTP client ─────────────────────────────────────────
        services.Configure<RamaJudicialOptions>(
            configuration.GetSection(RamaJudicialOptions.SectionName));

        services
            .AddHttpClient<IRamaJudicialClient, RamaJudicialClient>((sp, client) =>
            {
                var opts = sp.GetRequiredService<
                    Microsoft.Extensions.Options.IOptions<RamaJudicialOptions>>().Value;
                client.BaseAddress = new Uri(opts.BaseUrl);
            })
            .AddResilienceHandler("rama-judicial", (builder, context) =>
            {
                var opts = context.ServiceProvider.GetRequiredService<
                    Microsoft.Extensions.Options.IOptions<RamaJudicialOptions>>().Value;

                // Total timeout caps the full operation including all retries
                builder.AddTimeout(TimeSpan.FromSeconds(opts.TimeoutSeconds * 4));

                // Retry only on transient failures — never on 403/404/400
                builder.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = 2,
                    BackoffType = DelayBackoffType.Exponential,
                    Delay = TimeSpan.FromSeconds(1),
                    UseJitter = true,
                    ShouldHandle = static args =>
                    {
                        // Never retry definitive or WAF-blocked responses
                        var status = args.Outcome.Result?.StatusCode;
                        if (status is HttpStatusCode.Forbidden
                                   or HttpStatusCode.NotFound
                                   or HttpStatusCode.BadRequest)
                            return ValueTask.FromResult(false);

                        // Retry on exceptions (network, per-attempt timeout)
                        if (args.Outcome.Exception is not null)
                            return ValueTask.FromResult(true);

                        // Retry on 5xx or 408
                        return ValueTask.FromResult(
                            status == HttpStatusCode.RequestTimeout ||
                            (status.HasValue && (int)status.Value >= 500));
                    }
                });

                // Per-attempt timeout — innermost strategy
                builder.AddTimeout(TimeSpan.FromSeconds(opts.TimeoutSeconds));
            });
        // Catalog
        services.AddMemoryCache();
        services.AddScoped<ICatalogReader, CachedCatalogReader>();

        // ── Hangfire storage (separate 'hangfire' schema) ─────────────────────
        services.AddHangfire(cfg => cfg
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(
                opts => opts.UseNpgsqlConnection(connectionString),
                new PostgreSqlStorageOptions
                {
                    SchemaName = "hangfire",
                    PrepareSchemaIfNecessary = true,
                    JobExpirationCheckInterval = TimeSpan.FromHours(1),
                    InvisibilityTimeout = TimeSpan.FromMinutes(30),
                }));

        // ── Excel import (preview pipeline) ───────────────────────────────────
        services.AddOptions<ImportOptions>()
            .BindConfiguration(ImportOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddScoped<IExcelParser, ClosedXmlExcelParser>();
        services.AddSingleton<IImportPreviewCache, ImportPreviewCache>();

        // ── Sync engine state (sync_state KV: WAF cooldown + adaptive throttle) ─
        services.AddScoped<ISyncStateService, SyncStateService>();

        // ── Process repository / reader ───────────────────────────────────────
        services.AddScoped<IProcessRepository, ProcessRepository>();
        services.AddScoped<IProcessReader, ProcessReader>();
        services.AddScoped<IPartialFetchScheduler, NoOpPartialFetchScheduler>();
        services.AddScoped<IProcessPdfGenerator, ProcessPdfGenerator>();

        return services;
    }
}
