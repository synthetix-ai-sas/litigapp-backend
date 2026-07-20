using System.Net;
using Hangfire;
using Hangfire.PostgreSql;
using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Imports;
using LitigApp.Application.Features.Notifications;
using LitigApp.Infrastructure.Catalog;
using LitigApp.Infrastructure.Imports;
using LitigApp.Infrastructure.ExternalApis.RamaJudicial;
using LitigApp.Infrastructure.Identity;
using LitigApp.Infrastructure.Notifications.Email;
using LitigApp.Infrastructure.Notifications.Templates;
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
using Resend;

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

        // Cap the Npgsql pool so api + worker together stay under the database connection
        // limit (Supabase free-tier session pooler = 15). Budget: api pool + worker pool +
        // headroom ≤ the plan's limit. Overridable per environment; null → Npgsql default (100).
        var maxPoolSize = configuration.GetValue<int?>("Database:MaxPoolSize");
        if (maxPoolSize is int poolSize)
        {
            connectionString = new Npgsql.NpgsqlConnectionStringBuilder(connectionString)
            {
                MaxPoolSize = poolSize,
            }.ConnectionString;
        }

        services.AddDbContext<AppDbContext>(options =>
            options
                .UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention());

        // Hangfire storage can live on a SEPARATE Postgres instance (e.g. its own free-tier
        // Supabase project) so its server processes (workers + internal dispatchers:
        // ServerWatchdog, ExpirationManager, CountersAggregator, DelayedJobScheduler,
        // RecurringJobScheduler, ...) don't compete with EF Core for the SAME 15-connection
        // budget. Optional: falls back to the app's own (already pool-capped) connection
        // string when ConnectionStrings:HangfireStorage isn't set — fully backward-compatible.
        var hangfireConnectionString = configuration.GetConnectionString("HangfireStorage");
        if (hangfireConnectionString is not null)
        {
            if (maxPoolSize is int hangfirePoolSize)
            {
                hangfireConnectionString = new Npgsql.NpgsqlConnectionStringBuilder(hangfireConnectionString)
                {
                    MaxPoolSize = hangfirePoolSize,
                }.ConnectionString;
            }
        }
        else
        {
            hangfireConnectionString = connectionString;
        }

        // IEmailSender (Resend) is registered below, in the notifications section.

        // Both roles: the api reads the current user from the HTTP context; the worker
        // registers it so BulkImportJob can build the shared ProcessCreationService.
        // (On the worker there is no HttpContext → UserId is null, but the bulk-import
        // path passes userId explicitly and never reads the current user.)
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Identity (UserManager/RoleManager) — BOTH roles: IIdentityService.GetUserProfileAsync
        // is used by INotificationDispatchService, which runs on both roles (the "notifications"
        // queue is drained by api AND worker, per blueprint §14). JWT issuance/validation and the
        // auth command handlers below stay api-only — the worker never issues/validates tokens.
        services.AddScoped<IIdentityService, IdentityService>();
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

        // Token lifetime driven by AuthOptions so the email template and the real
        // expiry always agree — change appsettings "Auth:TokenLifespanMinutes" to move both.
        services.AddOptions<DataProtectionTokenProviderOptions>()
            .Configure<Microsoft.Extensions.Options.IOptions<LitigApp.Application.Features.Auth.AuthOptions>>(
                (tokenOptions, authOpts) =>
                    tokenOptions.TokenLifespan = TimeSpan.FromMinutes(authOpts.Value.TokenLifespanMinutes));

        if (!isWorker)
        {
            services.AddOptions<JwtOptions>()
                .BindConfiguration(JwtOptions.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddOptions<LitigApp.Infrastructure.Identity.LegalOptions>()
                .BindConfiguration(LitigApp.Infrastructure.Identity.LegalOptions.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddOptions<LitigApp.Application.Features.Auth.AuthOptions>()
                .BindConfiguration(LitigApp.Application.Features.Auth.AuthOptions.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddScoped<IJwtTokenService, JwtTokenService>();
            services.AddScoped<IAuthRepository, AuthRepository>();
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

        // ── Hangfire storage (separate 'hangfire' schema; possibly a separate Postgres
        //    instance too — see hangfireConnectionString above) ─────────────────────
        services.AddHangfire(cfg => cfg
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(
                opts => opts.UseNpgsqlConnection(hangfireConnectionString),
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
        services.AddScoped<IImportJobRepository, ImportJobRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<INotificationLogRepository, NotificationLogRepository>();

        // ── Email notifications (blueprint §10.4) ─────────────────────────────
        // Singleton: stateless, and caches parsed Scriban templates across the app lifetime.
        services.AddSingleton<IEmailTemplateRenderer, ScribanEmailTemplateRenderer>();

        services.AddOptions<ResendSenderOptions>()
            .BindConfiguration(ResendSenderOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<NotificationsOptions>()
            .BindConfiguration(NotificationsOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // RESEND_APITOKEN is read directly (not Resend:ApiKey) — the Resend package's own
        // options type, configured exactly as the SDK expects, separate from our own
        // ResendSenderOptions (FromAddress/FromName/DevRedirectTo) above.
        services.Configure<ResendClientOptions>(o =>
            o.ApiToken = Environment.GetEnvironmentVariable("RESEND_APITOKEN") ?? string.Empty);

        services.AddHttpClient<ResendClient>()
            .AddResilienceHandler("resend", builder =>
            {
                // Retry only transient failures; EmailSendAsync's idempotencyKey (set by
                // NotificationDispatchService from the outbox row id) makes a retried POST
                // safe — Resend itself dedupes repeated sends with the same key.
                builder.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    BackoffType = DelayBackoffType.Exponential,
                    Delay = TimeSpan.FromSeconds(1),
                    UseJitter = true,
                });
                builder.AddTimeout(TimeSpan.FromSeconds(15));
            });
        services.AddTransient<IResend, ResendClient>();
        services.AddScoped<IEmailSender, ResendEmailSender>();

        return services;
    }
}
