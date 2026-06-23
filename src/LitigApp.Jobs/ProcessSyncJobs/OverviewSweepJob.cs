using Hangfire;
using LitigApp.Application.Common.Abstractions;
using LitigApp.Domain.Processes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LitigApp.Jobs.ProcessSyncJobs;

[Queue("overview_sweep")]
[DisableConcurrentExecution(timeoutInSeconds: 10 * 60)]
public sealed class OverviewSweepJob(
    IProcessRepository processRepository,
    IRamaJudicialClient ramaClient,
    IOptions<SweepOptions> sweepOptions,
    IDateTimeProvider clock,
    ILogger<OverviewSweepJob> logger)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        var opts = sweepOptions.Value;
        var startedAt = clock.UtcNow;

        logger.LogInformation(
            "OverviewSweepJob starting. BatchSize={BatchSize} MinimumHoursBetweenSyncs={Hours}h",
            opts.BatchSize, opts.MinimumHoursBetweenSyncsPerProcess);

        var processes = await processRepository.GetEligibleForOverviewSweepAsync(
            opts.BatchSize,
            TimeSpan.FromHours(opts.MinimumHoursBetweenSyncsPerProcess),
            ct);

        logger.LogInformation("OverviewSweepJob loaded {Count} eligible processes.", processes.Count);

        int processed = 0, changed = 0, noChange = 0, notFound = 0, errors = 0;
        bool wafTriggered = false;

        foreach (var process in processes)
        {
            if (ct.IsCancellationRequested || wafTriggered) break;

            process.LastSyncAttemptAt = DateTimeOffset.UtcNow;

            var result = await ramaClient.GetOverviewByFileNumberAsync(process.FileNumber, ct);

            if (result.IsSuccess)
            {
                var overview = result.Value;
                if (overview is null)
                {
                    process.SyncPhase = "idle";
                    process.SyncStatus = ProcessSyncStatus.NotFound;
                    notFound++;
                }
                else if (overview.LastActionDate.HasValue &&
                         new DateTimeOffset(overview.LastActionDate.Value, TimeSpan.Zero) > process.LastCourtActionAt)
                {
                    process.SyncPhase = "pending_actions";
                    process.LastCourtActionAt = new DateTimeOffset(overview.LastActionDate.Value, TimeSpan.Zero);
                    process.ExternalProcessId ??= overview.ExternalProcessId;
                    process.ExternalConnectionId ??= overview.ExternalConnectionId;
                    changed++;
                }
                else
                {
                    process.SyncPhase = "idle";
                    process.SyncStatus = ProcessSyncStatus.Ok;
                    process.LastSyncedAt = DateTimeOffset.UtcNow;
                    process.ExternalProcessId ??= overview.ExternalProcessId;
                    process.ExternalConnectionId ??= overview.ExternalConnectionId;
                    noChange++;
                }

                processed++;
            }
            else
            {
                var failure = result.Failure!;

                switch (failure.Kind)
                {
                    case FailureKind.NotFound:
                        process.SyncPhase = "idle";
                        process.SyncStatus = ProcessSyncStatus.NotFound;
                        notFound++;
                        processed++;
                        break;

                    case FailureKind.WafBlocked:
                        logger.LogWarning(
                            "OverviewSweepJob: WAF 403 on FileNumber={FileNumber}. Aborting run — WAF cooldown gate (waf_blocked_until) will be activated in 3.C.",
                            process.FileNumber);
                        // Don't mark this process — leave it in its current sync_phase
                        // so the next run retries it. waf_blocked_until is 3.C scope.
                        wafTriggered = true;
                        break;

                    case FailureKind.Transient:
                        process.SyncStatus = ProcessSyncStatus.Error;
                        process.SyncAttempts++;
                        process.SyncError = failure.Message;
                        errors++;
                        processed++;
                        logger.LogError(
                            "OverviewSweepJob: Transient error on FileNumber={FileNumber} (attempt {Attempts}): {Message}",
                            process.FileNumber, process.SyncAttempts, failure.Message);
                        break;

                    default:
                        process.SyncStatus = ProcessSyncStatus.Error;
                        process.SyncAttempts++;
                        process.SyncError = failure.Message;
                        errors++;
                        processed++;
                        logger.LogError(
                            "OverviewSweepJob: Failure Kind={Kind} on FileNumber={FileNumber}: {Message}",
                            failure.Kind, process.FileNumber, failure.Message);
                        break;
                }
            }

            await processRepository.SaveChangesAsync(ct);
        }

        var elapsed = clock.UtcNow - startedAt;
        logger.LogInformation(
            "OverviewSweepJob finished in {ElapsedMs:0}ms. " +
            "Processed={Processed} Changed={Changed} NoChange={NoChange} NotFound={NotFound} Errors={Errors} WafAborted={WafTriggered}",
            elapsed.TotalMilliseconds, processed, changed, noChange, notFound, errors, wafTriggered);
    }
}
