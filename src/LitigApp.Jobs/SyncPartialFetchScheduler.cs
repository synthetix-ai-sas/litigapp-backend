using LitigApp.Application.Common.Abstractions;

namespace LitigApp.Jobs;

/// <summary>
/// Real <see cref="IPartialFetchScheduler"/> (replaces the NoOp): when synchronous creation
/// leaves a process partial, enqueue CompletePartialFetchJob via the sync job scheduler.
/// </summary>
internal sealed class SyncPartialFetchScheduler(ISyncJobScheduler scheduler) : IPartialFetchScheduler
{
    public void SchedulePartialCompletion(Guid processId) => scheduler.EnqueuePartialFetch(processId);
}
