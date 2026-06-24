using Hangfire;
using Microsoft.Extensions.Logging;

namespace LitigApp.Jobs.ProcessSyncJobs;

/// <summary>
/// Triggered per user that had changes in a sweep. PR3: enqueue-only stub.
/// The real aggregated digest (Resend email + outbox + idempotency guard via
/// notification_log) lands in 4.C''.
/// </summary>
/// <remarks>
/// INVARIANT (review finding A3): this stub MUST NOT advance any "last notified"
/// watermark or write notification_log. If it did, 4.C'' would treat already-detected
/// changes as already sent and the lawyer would never receive the email.
/// </remarks>
[Queue("notifications")]
public sealed class DispatchUserNotificationsJob(ILogger<DispatchUserNotificationsJob> logger)
{
    public Task RunAsync(string userId, CancellationToken ct = default)
    {
        logger.LogInformation(
            "DispatchUserNotificationsJob triggered for UserId={UserId}. Email dispatch is 4.C'' (stub).",
            userId);
        return Task.CompletedTask;
    }
}
