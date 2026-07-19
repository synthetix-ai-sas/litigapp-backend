using System.Text.Json;
using Hangfire;
using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Notifications.Dtos;
using LitigApp.Domain.Notifications;
using Microsoft.Extensions.Logging;

namespace LitigApp.Jobs.ProcessSyncJobs;

/// <summary>
/// Triggered per user that had changes in a sweep (blueprint §10.3). Builds ONE aggregated
/// digest — never one email per process — and dispatches it via
/// <see cref="INotificationDispatchService"/>.
/// </summary>
/// <remarks>
/// Idempotency: <see cref="INotificationLogRepository.GetLastEmailSentAtAsync"/> is the
/// watermark. Only processes changed AFTER the user's last successfully-sent digest are
/// included, so a retry (Hangfire or otherwise) after a successful send finds nothing new
/// and returns early — it never re-sends the same digest.
/// </remarks>
[Queue("notifications")]
public sealed class DispatchUserNotificationsJob(
    IProcessRepository processRepository,
    INotificationLogRepository notificationLogRepo,
    IOutboxRepository outboxRepo,
    INotificationDispatchService dispatchService,
    IDateTimeProvider clock,
    ILogger<DispatchUserNotificationsJob> logger)
{
    public async Task RunAsync(string userId, CancellationToken ct = default)
    {
        var now = new DateTimeOffset(clock.UtcNow, TimeSpan.Zero);

        // First-ever digest looks back only 24h (not all-time history) so a brand-new user
        // doesn't suddenly receive years of stale "changes".
        var lastNotifiedAt = await notificationLogRepo.GetLastEmailSentAtAsync(userId, ct) ?? now.AddDays(-1);

        var changed = await processRepository.GetChangedSinceAsync(userId, lastNotifiedAt, ct);
        if (changed.Count == 0)
        {
            logger.LogInformation(
                "DispatchUserNotificationsJob: no changes for UserId={UserId} since {Since:o}.", userId, lastNotifiedAt);
            return;
        }

        var payload = new UserDigestOutboxPayload(
            changed.Select(p => new UserDigestProcessPayload(
                p.ProcessId, p.FileNumber, p.LatestAction, p.LatestAnnotation, p.LastCourtActionAt)).ToList(),
            changed.Count,
            now);

        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EventType = "UserProcessesUpdated",
            Channel = "email",
            Payload = JsonSerializer.Serialize(payload),
            Status = "pending",
            CreatedAt = now,
        };

        await outboxRepo.InsertAsync(message, ct);
        await dispatchService.DispatchAsync(message, ct);

        logger.LogInformation(
            "DispatchUserNotificationsJob: UserId={UserId} digest dispatched. TotalChanged={Total} Status={Status}.",
            userId, changed.Count, message.Status);
    }
}
