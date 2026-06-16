using LitigApp.Application.Common.Abstractions;
using Microsoft.Extensions.Logging;

namespace LitigApp.Infrastructure.Processes;

/// <summary>
/// No-op scheduler for the MVP. The partial-completion state lives durably on the
/// process (sync_phase = "pending_partial_completion"); the sync engine completes it.
/// When Hangfire's CompletePartialFetchJob lands, replace the body with an enqueue.
/// </summary>
internal sealed class NoOpPartialFetchScheduler(ILogger<NoOpPartialFetchScheduler> logger)
    : IPartialFetchScheduler
{
    public void SchedulePartialCompletion(Guid processId) =>
        logger.LogInformation(
            "Process {ProcessId} created with partial data; left as sync_phase=pending_partial_completion " +
            "for the sync engine to complete.", processId);
}
