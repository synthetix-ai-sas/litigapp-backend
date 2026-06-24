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
    ISyncStateService syncState,
    IOptions<SweepOptions> sweepOptions,
    IOptions<WafOptions> wafOptions,
    IDateTimeProvider clock,
    ILogger<OverviewSweepJob> logger)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        var opts = sweepOptions.Value;
        var waf = wafOptions.Value;
        var now = new DateTimeOffset(clock.UtcNow, TimeSpan.Zero);
        var startedAt = clock.UtcNow;

        // WAF cooldown gate — skip the entire run while blocked (blueprint §10.2).
        var blockedUntil = await syncState.GetWafBlockedUntilAsync(ct);
        if (blockedUntil is { } until && until > now)
        {
            logger.LogInformation(
                "OverviewSweepJob skipped — WAF cooldown active until {Until:o}.", until);
            return;
        }

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

            process.LastSyncAttemptAt = now;

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
                    process.LastSyncedAt = now;
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
                    {
                        // Atomically persist the cooldown (same DbContext also flushes this
                        // process's LastSyncAttemptAt, which correctly records the attempt).
                        var cooldownUntil = now.AddMinutes(waf.CooldownMinutesOnBlock);
                        await syncState.SetWafBlockedUntilAsync(
                            cooldownUntil,
                            $"WAF 403 on overview FileNumber={process.FileNumber}",
                            ct);
                        logger.LogWarning(
                            "OverviewSweepJob: WAF 403 on FileNumber={FileNumber}. Cooldown set until {Until:o}. " +
                            "Aborting run; remaining processes left untouched (re-prioritized next run).",
                            process.FileNumber, cooldownUntil);
                        // Leave sync_phase unchanged. Remaining processes keep older
                        // LastSyncAttemptAt and sort first on the next eligible run.
                        wafTriggered = true;
                        break;
                    }

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
