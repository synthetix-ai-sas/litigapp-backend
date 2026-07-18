using Hangfire;
using LitigApp.Jobs.ProcessSyncJobs;

namespace LitigApp.Jobs;

public static class HangfireConfiguration
{
    // Uses IRecurringJobManager (DI-scoped per container) instead of the static RecurringJob
    // to avoid a global-state race condition when multiple WebApplicationFactory instances
    // initialize in parallel during integration tests.
    public static void RegisterRecurringJobs(IRecurringJobManager manager, SweepOptions sweepOpts)
    {
        var overviewCron = $"*/{sweepOpts.OverviewIntervalMinutes} * * * *";

        manager.AddOrUpdate<OverviewSweepJob>(
            recurringJobId: "overview-sweep",
            queue: "overview_sweep",
            methodCall: job => job.RunAsync(CancellationToken.None),
            cronExpression: overviewCron,
            options: new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

        // Hourly, NOT every 5 min (blueprint §11 Step 11) — recovers outbox rows a normal
        // send left 'pending'/'processing'. Raw expression (not Hangfire's Cron helper,
        // which is obsolete in this Hangfire version).
        manager.AddOrUpdate<NotificationFallbackSweepJob>(
            recurringJobId: "notification-fallback-sweep",
            queue: "notifications",
            methodCall: job => job.RunAsync(CancellationToken.None),
            cronExpression: "0 * * * *",
            options: new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
    }
}
