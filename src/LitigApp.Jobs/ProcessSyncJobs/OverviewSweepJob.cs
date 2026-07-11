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
    ISyncJobScheduler scheduler,
    ISyncDelay syncDelay,
    IOptions<SweepOptions> sweepOptions,
    IOptions<ThrottleOptions> throttleOptions,
    IOptions<WafOptions> wafOptions,
    IDateTimeProvider clock,
    ILogger<OverviewSweepJob> logger)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        var opts = sweepOptions.Value;
        var waf = wafOptions.Value;
        var throttleOpts = throttleOptions.Value;
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

        // Adaptive throttle (decision D1): the job paces itself using this value from
        // sync_state; the HTTP client only enforces a minimal safety floor.
        var throttleSeconds = await syncState.GetOverviewThrottleSecondsAsync(ct);

        logger.LogInformation(
            "OverviewSweepJob starting. BatchSize={BatchSize} MinimumHoursBetweenSyncs={Hours}h Throttle={Throttle}s",
            opts.BatchSize, opts.MinimumHoursBetweenSyncsPerProcess, throttleSeconds);

        var processes = await processRepository.GetEligibleForOverviewSweepAsync(
            opts.BatchSize,
            TimeSpan.FromHours(opts.MinimumHoursBetweenSyncsPerProcess),
            ct);

        logger.LogInformation("OverviewSweepJob loaded {Count} eligible processes.", processes.Count);

        int processed = 0, changed = 0, noChange = 0, notFound = 0, errors = 0;
        bool wafTriggered = false;

        var first = true;
        foreach (var process in processes)
        {
            if (ct.IsCancellationRequested || wafTriggered) break;

            if (!first)
                await syncDelay.WaitAsync(PaceDelay(throttleSeconds, throttleOpts), ct);
            first = false;

            process.LastSyncAttemptAt = now;

            var result = await ramaClient.GetOverviewByFileNumberAsync(process.FileNumber, ct);

            if (result.IsSuccess)
            {
                var overview = result.Value;
                if (overview is null)
                {
                    process.SyncPhase = ProcessSyncPhase.Idle;
                    process.SyncStatus = ProcessSyncStatus.NotFound;
                    notFound++;
                }
                else if (overview.IsPrivate)
                {
                    // Private processes can't expose actions (the actions endpoint 404s). Keep
                    // them idle and NEVER enqueue ActionsSweep (blueprint "Manejo de procesos
                    // privados"). Guard runs before change-detection so a stray last-action date
                    // can't promote them to pending_actions.
                    process.IsPrivate = true;
                    process.SyncPhase = ProcessSyncPhase.Idle;
                    process.SyncStatus = ProcessSyncStatus.Ok;
                    process.LastSyncedAt = now;
                    process.ExternalProcessId ??= overview.ExternalProcessId;
                    process.ExternalConnectionId ??= overview.ExternalConnectionId;
                    noChange++;
                }
                else if (overview.LastActionDate.HasValue &&
                         new DateTimeOffset(overview.LastActionDate.Value, TimeSpan.Zero) > process.LastCourtActionAt)
                {
                    process.SyncPhase = ProcessSyncPhase.PendingActions;
                    process.LastCourtActionAt = new DateTimeOffset(overview.LastActionDate.Value, TimeSpan.Zero);
                    process.ExternalProcessId ??= overview.ExternalProcessId;
                    process.ExternalConnectionId ??= overview.ExternalConnectionId;
                    changed++;
                }
                else
                {
                    process.SyncPhase = ProcessSyncPhase.Idle;
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
                        process.SyncPhase = ProcessSyncPhase.Idle;
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

        // Trigger ActionsSweep if this run marked any process pending_actions.
        if (changed > 0)
        {
            logger.LogInformation("OverviewSweepJob enqueuing ActionsSweep ({Changed} changed).", changed);
            scheduler.EnqueueActionsSweep();
        }

        await AdjustThrottleAsync(throttleSeconds, wafTriggered, processed, waf, throttleOpts, ct);

        var elapsed = clock.UtcNow - startedAt;
        logger.LogInformation(
            "OverviewSweepJob finished in {ElapsedMs:0}ms. " +
            "Processed={Processed} Changed={Changed} NoChange={NoChange} NotFound={NotFound} Errors={Errors} WafAborted={WafTriggered}",
            elapsed.TotalMilliseconds, processed, changed, noChange, notFound, errors, wafTriggered);
    }

    /// <summary>
    /// Adaptive throttle (blueprint §10.1 step 5): a blocked run slows down (+2, capped at
    /// the emergency ceiling); a clean run that processed enough speeds up (-1, floored at
    /// the configured minimum). Otherwise unchanged.
    /// </summary>
    private async Task AdjustThrottleAsync(
        int current, bool blocked, int processed, WafOptions waf, ThrottleOptions throttleOpts, CancellationToken ct)
    {
        var next = blocked
            ? Math.Min(current + 2, waf.EmergencyMaxThrottleSeconds)
            : processed >= waf.ConsecutiveSuccessesToSpeedUp
                ? Math.Max(current - 1, throttleOpts.OverviewIntervalSecondsMin)
                : current;

        if (next == current)
            return;

        await syncState.SetOverviewThrottleSecondsAsync(next, ct);
        logger.LogInformation("OverviewSweepJob adaptive throttle {Old}s -> {New}s.", current, next);
    }

    private static TimeSpan PaceDelay(int throttleSeconds, ThrottleOptions opts)
    {
        var jitterMaxMs = Math.Max(1, (opts.OverviewIntervalSecondsMax - opts.OverviewIntervalSecondsMin) * 1000);
        return TimeSpan.FromSeconds(throttleSeconds) + TimeSpan.FromMilliseconds(Random.Shared.Next(0, jitterMaxMs));
    }
}
