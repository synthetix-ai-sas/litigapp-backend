using LitigApp.Application.Features.Notifications.Dtos;
using LitigApp.Domain.Catalog;
using LitigApp.Domain.Processes;

namespace LitigApp.Application.Common.Abstractions;

public interface IProcessRepository
{
    /// <summary>
    /// Active processes for the user whose court action changed after <paramref name="since"/>
    /// (digest source, blueprint §10.3), most-recent action first. Each row carries its
    /// single latest <see cref="Domain.Processes.ProcessAction"/> (Action/Annotation) so the
    /// digest email shows accion+anotacion consistently from the same action row.
    /// </summary>
    Task<List<ChangedProcessDto>> GetChangedSinceAsync(string userId, DateTimeOffset since, CancellationToken ct);

    Task<List<Process>> GetEligibleForOverviewSweepAsync(
        int batchSize,
        TimeSpan minimumTimeBetweenSyncs,
        CancellationToken ct);

    /// <summary>Tracked batch of active processes in sync_phase='pending_actions', oldest attempt first.</summary>
    Task<List<Process>> GetPendingActionsAsync(int batchSize, CancellationToken ct);

    /// <summary>All persisted actions for a process (read-only) — for diff/dedupe and grouping context.</summary>
    Task<List<ProcessAction>> GetActionsAsync(Guid processId, CancellationToken ct);

    /// <summary>Stages new action rows to be persisted on the next SaveChanges.</summary>
    Task AddActionsAsync(IEnumerable<ProcessAction> actions, CancellationToken ct);

    /// <summary>Stages new subject rows to be persisted on the next SaveChanges.</summary>
    Task AddSubjectsAsync(IEnumerable<ProcessSubject> subjects, CancellationToken ct);

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

    /// <summary>Gets a tracked process by id (no owner filter) for system jobs. Null if not found.</summary>
    Task<Process?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>True if the process already has at least one subject row.</summary>
    Task<bool> HasSubjectsAsync(Guid processId, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}
