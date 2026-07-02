using Hangfire;
using LitigApp.Application.Common.Abstractions;
using Microsoft.Extensions.Logging;

namespace LitigApp.Jobs.ProcessSyncJobs;

/// <summary>
/// Stub — triggered by BulkImportJob after completion.
/// Full email dispatch (Resend + template) is implemented in 4.C'' (Step 11).
/// The outbox row is already written by BulkImportJob; this job picks it up when ready.
/// </summary>
[Queue("notifications")]
public sealed class DispatchImportCompleteJob(
    IImportJobRepository importRepo,
    ILogger<DispatchImportCompleteJob> logger)
{
    public async Task RunAsync(Guid importJobId, CancellationToken ct = default)
    {
        var job = await importRepo.GetByIdAsync(importJobId, ct);
        if (job is null)
        {
            logger.LogWarning("DispatchImportCompleteJob: importJobId={Id} not found.", importJobId);
            return;
        }

        // 4.C'': send ImportComplete email via Resend using the outbox row.
        logger.LogInformation(
            "DispatchImportCompleteJob stub: importJobId={Id} success={S} errors={E}. " +
            "Email dispatch will be implemented in Step 11 (4.C'').",
            importJobId, job.SuccessCount, job.ErrorCount);
    }
}
