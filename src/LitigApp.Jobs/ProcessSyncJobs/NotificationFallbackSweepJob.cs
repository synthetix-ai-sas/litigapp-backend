using Hangfire;
using LitigApp.Application.Common.Abstractions;
using Microsoft.Extensions.Logging;

namespace LitigApp.Jobs.ProcessSyncJobs;

/// <summary>
/// Recurring HOURLY sweep (blueprint §11 Step 11, NOT every 5 min) that retries outbox rows
/// still 'pending'/'processing' after a normal send should have finished. The 10-minute
/// minimum age avoids racing an in-flight send that just hasn't completed yet.
/// </summary>
[Queue("notifications")]
[DisableConcurrentExecution(timeoutInSeconds: 30 * 60)]
public sealed class NotificationFallbackSweepJob(
    IOutboxRepository outboxRepo,
    INotificationDispatchService dispatchService,
    ILogger<NotificationFallbackSweepJob> logger)
{
    private static readonly TimeSpan MinAge = TimeSpan.FromMinutes(10);
    private const int BatchSize = 50;

    public async Task RunAsync(CancellationToken ct = default)
    {
        var orphans = await outboxRepo.GetPendingOlderThanAsync(MinAge, BatchSize, ct);
        if (orphans.Count == 0)
        {
            logger.LogInformation("NotificationFallbackSweepJob: no orphaned outbox rows.");
            return;
        }

        logger.LogInformation("NotificationFallbackSweepJob: retrying {Count} orphaned outbox rows.", orphans.Count);

        foreach (var message in orphans)
        {
            if (ct.IsCancellationRequested) break;
            await dispatchService.DispatchAsync(message, ct);
        }
    }
}
