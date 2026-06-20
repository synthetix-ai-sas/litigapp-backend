namespace LitigApp.Application.Common.Abstractions;

/// <summary>
/// Hook invoked when a process is created with incomplete data (sync_status = "partial").
/// The durable source of truth is the persisted sync_phase = "pending_partial_completion"
/// (covered by idx_processes_sync_phase); the sync engine (3.C) picks those up regardless.
/// The default <c>NoOp</c> implementation does nothing today; once Hangfire's
/// CompletePartialFetchJob exists it becomes a one-line enqueue.
/// </summary>
public interface IPartialFetchScheduler
{
    void SchedulePartialCompletion(Guid processId);
}
