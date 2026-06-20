using LitigApp.Domain.Catalog;
using LitigApp.Domain.Processes;

namespace LitigApp.Application.Common.Abstractions;

public interface IProcessRepository
{
    Task<List<Process>> GetEligibleForOverviewSweepAsync(
        int batchSize,
        TimeSpan minimumTimeBetweenSyncs,
        CancellationToken ct);

    /// <summary>True if the user already has a process with this file number (active or not).</summary>
    Task<bool> ExistsAsync(string userId, string fileNumber, CancellationToken ct);

    /// <summary>True if the user has an import job in progress (pending/running/paused).</summary>
    Task<bool> HasActiveImportAsync(string userId, CancellationToken ct);

    /// <summary>Looks up a court by id (for the wizard). Null if not found.</summary>
    Task<Court?> FindCourtAsync(Guid courtId, CancellationToken ct);

    /// <summary>Looks up a court by its 12-char official code. Null if not in the catalog.</summary>
    Task<Court?> FindCourtByOfficialCodeAsync(string officialCode, CancellationToken ct);

    /// <summary>Adds a new process graph (with subjects/actions) to be persisted on SaveChanges.</summary>
    Task AddAsync(Process process, CancellationToken ct);

    /// <summary>Gets a tracked process owned by the user, or null. Use for mutations.</summary>
    Task<Process?> GetOwnedAsync(Guid id, string userId, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}
