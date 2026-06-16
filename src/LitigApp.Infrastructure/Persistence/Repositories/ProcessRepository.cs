using LitigApp.Application.Common.Abstractions;
using LitigApp.Domain.Catalog;
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

    public Task<bool> ExistsAsync(string userId, string fileNumber, CancellationToken ct) =>
        db.Processes.AsNoTracking().AnyAsync(p => p.UserId == userId && p.FileNumber == fileNumber, ct);

    public Task<bool> HasActiveImportAsync(string userId, CancellationToken ct) =>
        db.ImportJobs.AsNoTracking().AnyAsync(
            j => j.UserId == userId &&
                 (j.Status == "pending" || j.Status == "running" || j.Status == "paused"),
            ct);

    public Task<Court?> FindCourtAsync(Guid courtId, CancellationToken ct) =>
        db.Courts.AsNoTracking().FirstOrDefaultAsync(c => c.Id == courtId, ct);

    public Task<Court?> FindCourtByOfficialCodeAsync(string officialCode, CancellationToken ct) =>
        db.Courts.AsNoTracking().FirstOrDefaultAsync(c => c.OfficialCode == officialCode, ct);

    public async Task AddAsync(Process process, CancellationToken ct) =>
        await db.Processes.AddAsync(process, ct);

    public Task<Process?> GetOwnedAsync(Guid id, string userId, CancellationToken ct) =>
        db.Processes.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId, ct);

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
