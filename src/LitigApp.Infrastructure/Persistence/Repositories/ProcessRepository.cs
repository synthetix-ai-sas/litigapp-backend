using LitigApp.Application.Common.Abstractions;
using LitigApp.Domain.Processes;
using LitigApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LitigApp.Infrastructure.Persistence.Repositories;

internal sealed class ProcessRepository(AppDbContext db) : IProcessRepository
{
    public Task<List<Process>> GetEligibleForOverviewSweepAsync(
        int batchSize,
        TimeSpan minimumTimeBetweenSyncs,
        CancellationToken ct)
    {
        var cutoff = DateTimeOffset.UtcNow - minimumTimeBetweenSyncs;

        return db.Processes
            .Where(p => p.IsActive &&
                (p.SyncPhase == "pending_overview" ||
                 (p.SyncPhase == "idle" && (p.LastSyncedAt == null || p.LastSyncedAt < cutoff))))
            .OrderBy(p => p.LastSyncAttemptAt == null ? DateTimeOffset.MinValue : p.LastSyncAttemptAt.Value)
            .Take(batchSize)
            .ToListAsync(ct);  // tracked — we modify these entities immediately after
    }

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
