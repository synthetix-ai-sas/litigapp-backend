using LitigApp.Domain.Notifications;

namespace LitigApp.Application.Common.Abstractions;

public interface INotificationLogRepository
{
    /// <summary>
    /// Timestamp of the user's last successfully-sent email, or null if never sent.
    /// The idempotency watermark for the digest (blueprint §10.3): only processes changed
    /// AFTER this point are included in the next digest.
    /// </summary>
    Task<DateTimeOffset?> GetLastEmailSentAtAsync(string userId, CancellationToken ct);

    Task InsertAsync(NotificationLog log, CancellationToken ct);
}
