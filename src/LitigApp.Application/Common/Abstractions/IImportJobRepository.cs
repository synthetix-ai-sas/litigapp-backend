using LitigApp.Domain.Imports;

namespace LitigApp.Application.Common.Abstractions;

public interface IImportJobRepository
{
    Task<ImportJob> CreateAsync(ImportJob job, CancellationToken ct);
    Task<ImportJob?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<ImportJob?> GetActiveForUserAsync(string userId, CancellationToken ct);

    /// <summary>
    /// Returns the most recent job for the user if it is active (pending/running/paused)
    /// OR completed within <paramref name="completedWithinSeconds"/> seconds — for the 60-second
    /// frontend polling window (blueprint §9 step 5 GET /imports/active).
    /// </summary>
    Task<ImportJob?> GetActiveOrRecentAsync(string userId, int completedWithinSeconds, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}
