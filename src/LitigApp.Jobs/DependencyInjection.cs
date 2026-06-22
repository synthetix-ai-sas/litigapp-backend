using Hangfire;
using LitigApp.Jobs.ProcessSyncJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LitigApp.Jobs;

public static class DependencyInjection
{
    public static IServiceCollection AddJobs(
        this IServiceCollection services,
        IConfiguration configuration)
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
            opts.Queues =
            [
                "overview_sweep",
                "actions_sweep",
                "partial_fetch",
                "bulk_import",
                "notifications",
                "default"
            ];
            opts.WorkerCount = Math.Max(Environment.ProcessorCount * 2, 4);
        });

        services.AddScoped<OverviewSweepJob>();
        services.AddHostedService<HangfireRecurringJobsHostedService>();

        return services;
    }
}
