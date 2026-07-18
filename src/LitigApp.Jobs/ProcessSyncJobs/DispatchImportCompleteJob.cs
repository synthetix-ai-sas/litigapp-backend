using Hangfire;
using LitigApp.Application.Common.Abstractions;
using Microsoft.Extensions.Logging;

namespace LitigApp.Jobs.ProcessSyncJobs;

/// <summary>
/// Triggered by BulkImportJob after it inserts the ImportComplete outbox row. The row's
/// payload already carries everything needed (fileName, counts) — this job just loads it
/// and hands it to <see cref="INotificationDispatchService"/>.
/// </summary>
[Queue("notifications")]
public sealed class DispatchImportCompleteJob(
    IOutboxRepository outboxRepo,
    INotificationDispatchService dispatchService,
    ILogger<DispatchImportCompleteJob> logger)
{
    public async Task RunAsync(Guid outboxId, CancellationToken ct = default)
    {
        var message = await outboxRepo.GetByIdAsync(outboxId, ct);
        if (message is null)
        {
            logger.LogWarning("DispatchImportCompleteJob: outboxId={Id} not found.", outboxId);
            return;
        }

        await dispatchService.DispatchAsync(message, ct);

        logger.LogInformation(
            "DispatchImportCompleteJob: outboxId={Id} dispatched. Status={Status}.", outboxId, message.Status);
    }
}
