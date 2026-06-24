using Hangfire;
using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Processes.Sync;
using LitigApp.Domain.Processes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LitigApp.Jobs.ProcessSyncJobs;

/// <summary>
/// Triggered (non-recurring) sweep over processes in sync_phase='pending_actions'.
/// For each: fetch page 1 of actions, insert genuinely-new ones (idempotent dedupe by
/// external_action_id), group Auto+Fijación, mark attended=false, and enqueue one
/// notification dispatch per user with changes. Honors the WAF cooldown.
/// </summary>
[Queue("actions_sweep")]
[DisableConcurrentExecution(timeoutInSeconds: 30 * 60)]
public sealed class ActionsSweepJob(
    IProcessRepository processRepository,
    IRamaJudicialClient ramaClient,
    ISyncStateService syncState,
    ISyncJobScheduler scheduler,
    IOptions<SweepOptions> sweepOptions,
    IOptions<WafOptions> wafOptions,
    IDateTimeProvider clock,
    ILogger<ActionsSweepJob> logger)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        var opts = sweepOptions.Value;
        var waf = wafOptions.Value;
        var now = new DateTimeOffset(clock.UtcNow, TimeSpan.Zero);

        // Cooldown gate — reschedule (this is a triggered one-shot, not a recurring job).
        var blockedUntil = await syncState.GetWafBlockedUntilAsync(ct);
        if (blockedUntil is { } until && until > now)
        {
            logger.LogInformation("ActionsSweepJob deferred — WAF cooldown until {Until:o}.", until);
            scheduler.ScheduleActionsSweep(until - now);
            return;
        }

        var processes = await processRepository.GetPendingActionsAsync(opts.BatchSize, ct);
        logger.LogInformation("ActionsSweepJob loaded {Count} processes pending actions.", processes.Count);

        var changedUsers = new HashSet<string>(StringComparer.Ordinal);
        int success = 0, changed = 0, errors = 0;
        var wafTriggered = false;

        foreach (var process in processes)
        {
            if (ct.IsCancellationRequested)
                break;

            process.LastSyncAttemptAt = now;

            if (process.ExternalProcessId is not { } externalId)
            {
                MarkError(process, "Missing ExternalProcessId for actions fetch.");
                errors++;
                await processRepository.SaveChangesAsync(ct);
                continue;
            }

            var result = await ramaClient.GetFirstPageActionsAsync(externalId, ct);

            if (!result.IsSuccess)
            {
                var failure = result.Failure!;
                if (failure.Kind == FailureKind.WafBlocked)
                {
                    // Atomic cooldown write (also flushes this process's LastSyncAttemptAt).
                    var cooldownUntil = now.AddMinutes(waf.CooldownMinutesOnBlock);
                    await syncState.SetWafBlockedUntilAsync(
                        cooldownUntil, $"WAF 403 on actions FileNumber={process.FileNumber}", ct);
                    logger.LogWarning(
                        "ActionsSweepJob: WAF 403 on FileNumber={FileNumber}. Cooldown until {Until:o}. Aborting.",
                        process.FileNumber, cooldownUntil);
                    wafTriggered = true;
                    break;
                }

                MarkError(process, failure.Message);
                errors++;
                await processRepository.SaveChangesAsync(ct);
                continue;
            }

            var apiActions = result.Value ?? [];
            var existing = await processRepository.GetActionsAsync(process.Id, ct);
            var existingIds = existing.Select(a => a.ExternalActionId).ToHashSet();

            // Idempotent diff: only insert actions we don't already have (dedupe by external id),
            // not merely those above the consecutive watermark — survives a crashed prior run.
            var newActions = apiActions
                .Where(a => !existingIds.Contains(a.ExternalActionId))
                .Select(a => ProcessActionFactory.Create(a, process.Id, now))
                .ToList();

            if (newActions.Count > 0)
            {
                ActionGrouper.AssignGroups(newActions, existing);
                await processRepository.AddActionsAsync(newActions, ct);

                process.Attended = false;
                process.LastExternalConsecutive = Math.Max(
                    process.LastExternalConsecutive, newActions.Max(a => a.ConsecutiveNumber));
                process.CurrentStatus = apiActions.MaxBy(a => a.ActionNumber)!.ActionType;
                changedUsers.Add(process.UserId);
                changed++;
            }

            process.SyncPhase = "idle";
            process.SyncStatus = ProcessSyncStatus.Ok;
            process.SyncError = null;
            process.LastSyncedAt = now;
            success++;

            await processRepository.SaveChangesAsync(ct);
        }

        // Enqueue one aggregated dispatch per user with changes (send itself is 4.C'').
        foreach (var userId in changedUsers)
            scheduler.EnqueueUserNotifications(userId);

        if (wafTriggered)
        {
            // Resume the remaining pending_actions after the cooldown.
            scheduler.ScheduleActionsSweep(TimeSpan.FromMinutes(waf.CooldownMinutesOnBlock));
        }
        else if (processes.Count == opts.BatchSize && success > 0)
        {
            // Full batch with progress — more may remain; continue.
            logger.LogInformation("ActionsSweepJob enqueuing continuation (full batch).");
            scheduler.EnqueueActionsSweep();
        }

        logger.LogInformation(
            "ActionsSweepJob finished. Success={Success} Changed={Changed} Errors={Errors} " +
            "UsersNotified={Users} WafAborted={Waf}",
            success, changed, errors, changedUsers.Count, wafTriggered);
    }

    private static void MarkError(Process process, string message)
    {
        process.SyncStatus = ProcessSyncStatus.Error;
        process.SyncError = message;
        process.SyncAttempts++;
    }
}
