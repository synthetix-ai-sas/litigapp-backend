using Hangfire;
using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Processes.Sync;
using LitigApp.Domain.Processes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LitigApp.Jobs.ProcessSyncJobs;

/// <summary>
/// Completes a process left partial by synchronous creation (some of detail/subjects/actions
/// failed by WAF/transient). Retries ONLY the missing endpoints (inferred from persisted data),
/// honors the WAF cooldown, and never sends a notification (the lawyer already created it).
/// </summary>
[Queue("partial_fetch")]
[DisableConcurrentExecution(timeoutInSeconds: 10 * 60)]
public sealed class CompletePartialFetchJob(
    IProcessRepository processRepository,
    IRamaJudicialClient ramaClient,
    ISyncStateService syncState,
    ISyncJobScheduler scheduler,
    IOptions<WafOptions> wafOptions,
    IDateTimeProvider clock,
    ILogger<CompletePartialFetchJob> logger)
{
    public async Task RunAsync(Guid processId, CancellationToken ct = default)
    {
        var waf = wafOptions.Value;
        var now = new DateTimeOffset(clock.UtcNow, TimeSpan.Zero);

        var blockedUntil = await syncState.GetWafBlockedUntilAsync(ct);
        if (blockedUntil is { } until && until > now)
        {
            logger.LogInformation(
                "CompletePartialFetchJob deferred for {ProcessId} — WAF cooldown until {Until:o}.", processId, until);
            scheduler.SchedulePartialFetch(processId, until - now);
            return;
        }

        var process = await processRepository.GetByIdAsync(processId, ct);
        if (process is null || process.SyncPhase != ProcessSyncPhase.PendingPartialCompletion)
            return; // nothing to complete (idempotent)

        if (process.ExternalProcessId is not { } externalId)
        {
            // Overview itself failed at creation — we have no id to fetch with. Leave partial.
            logger.LogWarning(
                "CompletePartialFetchJob: {ProcessId} has no ExternalProcessId; cannot complete.", processId);
            return;
        }

        var missing = new List<string>();
        var wafHit = false;

        // detail (inferred missing when the metadata was never populated)
        if (process.ProcessClass is null)
        {
            var result = await ramaClient.GetDetailAsync(externalId, ct);
            if (result.IsSuccess && result.Value is { } detail)
                ApplyDetail(process, detail);
            else if (IsWaf(result.Failure))
                wafHit = true;
            else
                missing.Add("detalle");
        }

        // subjects
        if (!wafHit && !await processRepository.HasSubjectsAsync(processId, ct))
        {
            var result = await ramaClient.GetSubjectsAsync(externalId, ct);
            if (result.IsSuccess && result.Value is { } subjects)
            {
                var rows = subjects.Select(s => ProcessSubjectFactory.Create(s, process.Id, now)).ToList();
                await processRepository.AddSubjectsAsync(rows, ct);
            }
            else if (IsWaf(result.Failure))
            {
                wafHit = true;
            }
            else
            {
                missing.Add("sujetos");
            }
        }

        // actions
        if (!wafHit && (await processRepository.GetActionsAsync(processId, ct)).Count == 0)
        {
            var result = await ramaClient.GetFirstPageActionsAsync(externalId, ct);
            if (result.IsSuccess && result.Value is { } actions && actions.Count > 0)
            {
                var rows = actions.Select(a => ProcessActionFactory.Create(a, process.Id, now)).ToList();
                ActionGrouper.AssignGroups(rows, []);
                await processRepository.AddActionsAsync(rows, ct);

                process.LastExternalConsecutive = rows.Max(a => a.ConsecutiveNumber);
                process.CurrentStatus = actions.MaxBy(a => a.ActionNumber)!.ActionType;
            }
            else if (IsWaf(result.Failure))
            {
                wafHit = true;
            }
            else if (!result.IsSuccess)
            {
                missing.Add("actuaciones");
            }
        }

        if (wafHit)
        {
            var cooldownUntil = now.AddMinutes(waf.CooldownMinutesOnBlock);
            await syncState.SetWafBlockedUntilAsync(
                cooldownUntil, $"WAF 403 on partial fetch processId={processId}", ct);
            scheduler.SchedulePartialFetch(processId, TimeSpan.FromMinutes(waf.CooldownMinutesOnBlock));
            await processRepository.SaveChangesAsync(ct); // persist whatever we managed to fetch
            logger.LogWarning(
                "CompletePartialFetchJob: WAF 403 completing {ProcessId}. Cooldown until {Until:o}; left partial.",
                processId, cooldownUntil);
            return;
        }

        if (missing.Count == 0)
        {
            process.SyncPhase = ProcessSyncPhase.Idle;
            process.SyncStatus = ProcessSyncStatus.Ok;
            process.SyncError = null;
            process.LastSyncedAt = now;
        }
        else
        {
            process.SyncStatus = ProcessSyncStatus.Partial;
            process.SyncAttempts++;
            process.SyncError = $"No se pudo completar: {string.Join(", ", missing)}.";
        }

        await processRepository.SaveChangesAsync(ct);
        // No notification — the lawyer already created the process.
    }

    private static void ApplyDetail(Process process, ProcessDetailData detail)
    {
        process.ProcessType = detail.ProcessType;
        process.ProcessClass = detail.ProcessClass;
        process.ProcessSubclass = detail.ProcessSubclass;
        process.Resource = detail.Resource;
        process.JudgeName = detail.Rapporteur;
        process.FilingContent = detail.FilingContent;
    }

    private static bool IsWaf(RamaJudicialFailure? failure) => failure?.Kind == FailureKind.WafBlocked;
}
