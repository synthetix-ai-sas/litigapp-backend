using Hangfire;
using LitigApp.Jobs.ProcessSyncJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
#pragma warning disable CS0618 // Cron helpers not used here — using raw cron strings

namespace LitigApp.Jobs;

public static class HangfireConfiguration
{
    public static void RegisterRecurringJobs(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sweepOpts = scope.ServiceProvider
            .GetRequiredService<IOptions<SweepOptions>>().Value;

        var overviewCron = $"*/{sweepOpts.OverviewIntervalMinutes} * * * *";

        RecurringJob.AddOrUpdate<OverviewSweepJob>(
            recurringJobId: "overview-sweep",
            queue: "overview_sweep",
            methodCall: job => job.RunAsync(CancellationToken.None),
            cronExpression: overviewCron,
            options: new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

        // NotificationFallbackSweepJob — registered here as a placeholder (implementation in 3.C)
        // RecurringJob.AddOrUpdate<NotificationFallbackSweepJob>(
        //     "notification-fallback-sweep", "notifications",
        //     job => job.RunAsync(CancellationToken.None), Cron.Hourly());
    }
}
