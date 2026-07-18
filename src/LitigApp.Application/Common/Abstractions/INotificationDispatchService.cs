using LitigApp.Domain.Notifications;

namespace LitigApp.Application.Common.Abstractions;

/// <summary>
/// Renders and sends a single outbox row's notification (blueprint §10.3/§10.4), then
/// updates its status. Shared by DispatchUserNotificationsJob, DispatchImportCompleteJob,
/// and NotificationFallbackSweepJob so all 3 entry points behave identically. Never throws —
/// a send failure leaves the row 'pending' (with LastError/Attempts updated) for the fallback
/// sweep to retry; it does not mark it 'sent' or insert a notification_log row.
/// </summary>
public interface INotificationDispatchService
{
    Task DispatchAsync(OutboxMessage message, CancellationToken ct);
}
