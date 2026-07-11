using Hangfire;
using LitigApp.Application.Common.Abstractions;
using LitigApp.Jobs.ProcessSyncJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LitigApp.Jobs;

public static class DependencyInjection
{
    /// <summary>
    /// <paramref name="isWorker"/> controls Hangfire server sizing per section 14 of the blueprint:
    /// the api process only drains light/critical queues (notifications) so digest emails aren't
    /// delayed by sync load, while the worker process drains the heavy sync/import queues and owns
    /// recurring-job registration.
    /// </summary>
    public static IServiceCollection AddJobs(
        this IServiceCollection services,
        IConfiguration configuration,
        bool isWorker)
    {
        services.AddOptions<SweepOptions>()
            .BindConfiguration(SweepOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<ThrottleOptions>()
            .BindConfiguration(ThrottleOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<WafOptions>()
            .BindConfiguration(WafOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Hangfire server — queues matching section 10 of blueprint
        services.AddHangfireServer(opts =>
        {
            opts.Queues = isWorker
                ?
                [
                    "overview_sweep",
                    "actions_sweep",
                    "partial_fetch",
                    "bulk_import",
                    "notifications",
                    "default"
                ]
                : ["notifications", "default"];
            // Worker count drives how many jobs run (and thus DB connections) at once. Keep it
            // within the pool budget (see Database:MaxPoolSize) — the default is fine for a big
            // DB but must be lowered for Supabase free tier. The api role stays light.
            var configuredWorkers = configuration.GetValue<int?>("Hangfire:WorkerCount");
            opts.WorkerCount = isWorker
                ? (configuredWorkers is int w && w > 0 ? w : Math.Max(Environment.ProcessorCount * 2, 4))
                : 2;
        });

        services.AddScoped<ISyncJobScheduler, HangfireSyncJobScheduler>();
        services.AddScoped<IPartialFetchScheduler, SyncPartialFetchScheduler>();
        services.AddScoped<OverviewSweepJob>();
        services.AddScoped<ActionsSweepJob>();
        services.AddScoped<DispatchUserNotificationsJob>();
        services.AddScoped<BulkImportJob>();
        services.AddScoped<DispatchImportCompleteJob>();
        services.AddScoped<CompletePartialFetchJob>();

        if (isWorker)
        {
            services.AddHostedService<HangfireRecurringJobsHostedService>();
        }

        return services;
    }
}
