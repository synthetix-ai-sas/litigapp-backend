using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Notifications.Dtos;
using LitigApp.Domain.Catalog;
using LitigApp.Domain.Processes;
using LitigApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LitigApp.Infrastructure.Persistence.Repositories;

internal sealed class ProcessRepository(AppDbContext db) : IProcessRepository
{
    public Task<List<ChangedProcessDto>> GetChangedSinceAsync(
        string userId, DateTimeOffset since, CancellationToken ct) =>
        db.Processes.AsNoTracking()
            .Where(p => p.UserId == userId && p.IsActive &&
                        p.LastCourtActionAt != null && p.LastCourtActionAt > since)
            .OrderByDescending(p => p.LastCourtActionAt)
            .Select(p => new ChangedProcessDto(
                p.Id,
                p.FileNumber,
                p.LastCourtActionAt!.Value,
                p.Actions.OrderByDescending(a => a.ConsecutiveNumber).Select(a => a.Action).FirstOrDefault(),
                p.Actions.OrderByDescending(a => a.ConsecutiveNumber).Select(a => a.Annotation).FirstOrDefault()))
            .ToListAsync(ct);

    public Task<List<Process>> GetEligibleForOverviewSweepAsync(
        int batchSize,
        TimeSpan minimumTimeBetweenSyncs,
        CancellationToken ct)
    {
        var cutoff = DateTimeOffset.UtcNow - minimumTimeBetweenSyncs;

        return db.Processes
            .Where(p => p.IsActive &&
                (p.SyncPhase == ProcessSyncPhase.PendingOverview ||
                 (p.SyncPhase == ProcessSyncPhase.Idle && (p.LastSyncedAt == null || p.LastSyncedAt < cutoff))))
            .OrderBy(p => p.LastSyncAttemptAt == null ? DateTimeOffset.MinValue : p.LastSyncAttemptAt.Value)
            .Take(batchSize)
            .ToListAsync(ct);  // tracked — we modify these entities immediately after
    }

    public Task<List<Process>> GetPendingActionsAsync(int batchSize, CancellationToken ct) =>
        db.Processes
            .Where(p => p.IsActive && p.SyncPhase == ProcessSyncPhase.PendingActions)
            .OrderBy(p => p.LastSyncAttemptAt == null ? DateTimeOffset.MinValue : p.LastSyncAttemptAt.Value)
            .Take(batchSize)
            .ToListAsync(ct);  // tracked — modified in the sweep loop

    public Task<List<ProcessAction>> GetActionsAsync(Guid processId, CancellationToken ct) =>
        db.ProcessActions.AsNoTracking()
            .Where(a => a.ProcessId == processId)
            .ToListAsync(ct);

    public async Task AddActionsAsync(IEnumerable<ProcessAction> actions, CancellationToken ct) =>
        await db.ProcessActions.AddRangeAsync(actions, ct);

    public async Task AddSubjectsAsync(IEnumerable<ProcessSubject> subjects, CancellationToken ct) =>
        await db.ProcessSubjects.AddRangeAsync(subjects, ct);

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

    public Task<Process?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Processes.FirstOrDefaultAsync(p => p.Id == id, ct);  // tracked — mutated by the job

    public Task<bool> HasSubjectsAsync(Guid processId, CancellationToken ct) =>
        db.ProcessSubjects.AsNoTracking().AnyAsync(s => s.ProcessId == processId, ct);

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
