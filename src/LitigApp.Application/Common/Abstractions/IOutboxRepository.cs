using LitigApp.Domain.Notifications;

namespace LitigApp.Application.Common.Abstractions;

public interface IOutboxRepository
{
    /// <summary>Inserts and immediately persists (self-contained — no separate SaveChangesAsync needed).</summary>
    Task InsertAsync(OutboxMessage message, CancellationToken ct);

    /// <summary>Tracked lookup — callers mutate Status/Attempts/LastError/ProcessedAt then call SaveChangesAsync.</summary>
    Task<OutboxMessage?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Tracked rows still 'pending'/'processing' older than <paramref name="minAge"/> — orphans
    /// a normal send didn't finish (NotificationFallbackSweepJob, blueprint §11 hourly cron).
    /// </summary>
    Task<List<OutboxMessage>> GetPendingOlderThanAsync(TimeSpan minAge, int batchSize, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}
