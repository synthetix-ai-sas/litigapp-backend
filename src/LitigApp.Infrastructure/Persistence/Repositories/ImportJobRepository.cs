using LitigApp.Application.Common.Abstractions;
using LitigApp.Domain.Imports;
using LitigApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LitigApp.Infrastructure.Persistence.Repositories;

internal sealed class ImportJobRepository(AppDbContext db) : IImportJobRepository
{
    public async Task<ImportJob> CreateAsync(ImportJob job, CancellationToken ct)
    {
        await db.ImportJobs.AddAsync(job, ct);
        await db.SaveChangesAsync(ct);
        return job;
    }

    public Task<ImportJob?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.ImportJobs.FirstOrDefaultAsync(j => j.Id == id, ct);

    public Task<ImportJob?> GetActiveForUserAsync(string userId, CancellationToken ct) =>
        db.ImportJobs.FirstOrDefaultAsync(
            j => j.UserId == userId &&
                 (j.Status == ImportStatus.Pending ||
                  j.Status == ImportStatus.Running ||
                  j.Status == ImportStatus.Paused),
            ct);

    public Task<ImportJob?> GetActiveOrRecentAsync(
        string userId, int completedWithinSeconds, CancellationToken ct)
    {
        var cutoff = DateTimeOffset.UtcNow.AddSeconds(-completedWithinSeconds);
        return db.ImportJobs
            .Where(j => j.UserId == userId &&
                        (j.Status == ImportStatus.Pending ||
                         j.Status == ImportStatus.Running ||
                         j.Status == ImportStatus.Paused ||
                         (j.Status == ImportStatus.Completed && j.CompletedAt >= cutoff)))
            .OrderByDescending(j => j.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
