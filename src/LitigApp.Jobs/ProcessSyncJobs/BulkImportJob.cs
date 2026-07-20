using System.Text.Json;
using System.Text.RegularExpressions;
using Hangfire;
using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Imports;
using LitigApp.Application.Features.Notifications.Dtos;
using LitigApp.Domain.Imports;
using LitigApp.Domain.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LitigApp.Jobs.ProcessSyncJobs;

/// <summary>
/// Radicado-first bulk import (blueprint §9 Step 9, BulkImportJob box).
/// One job per import; processes rows sequentially to avoid WAF.
/// </summary>
[Queue("bulk_import")]
[DisableConcurrentExecution(timeoutInSeconds: 60 * 60)] // imports can take up to 1h
public sealed class BulkImportJob(
    IImportJobRepository importRepo,
    IProcessCreator processCreator,
    ISyncStateService syncState,
    ISyncJobScheduler scheduler,
    ISyncDelay syncDelay,
    IOutboxRepository outboxRepo,
    IOptions<ThrottleOptions> throttleOptions,
    IOptions<WafOptions> wafOptions,
    IDateTimeProvider clock,
    ILogger<BulkImportJob> logger)
{
    // Regex to strip everything that is not a decimal digit.
    private static readonly Regex NonDigit = new(@"\D", RegexOptions.Compiled);

    private static readonly JsonSerializerOptions PayloadJson = new() { PropertyNameCaseInsensitive = true };

    public async Task RunAsync(Guid importJobId, CancellationToken ct = default)
    {
        var throttle = throttleOptions.Value;
        var waf = wafOptions.Value;
        var now = new DateTimeOffset(clock.UtcNow, TimeSpan.Zero);

        var job = await importRepo.GetByIdAsync(importJobId, ct);
        if (job is null)
        {
            logger.LogWarning("BulkImportJob: importJobId={Id} not found.", importJobId);
            return;
        }

        // The confirmed rows are persisted on the job (spec A1), so the import survives the
        // API/worker process split and WAF pause/resume. A missing payload is unexpected.
        var rows = DeserializePayload(job.PreviewPayload);
        if (rows is null)
        {
            await FailJobAsync(job, "preview_missing", ct);
            return;
        }

        job.Status = ImportStatus.Running;
        job.TotalRows = rows.Count;
        await importRepo.SaveChangesAsync(ct);

        var errors = new List<ImportRowError>();
        var first = true;

        for (var i = 0; i < rows.Count; i++)
        {
            if (ct.IsCancellationRequested) break;

            // WAF cooldown gate — pause the job and re-enqueue after cooldown.
            var blockedUntil = await syncState.GetWafBlockedUntilAsync(ct);
            if (blockedUntil is { } until && until > now)
            {
                logger.LogWarning(
                    "BulkImportJob {Id}: WAF cooldown until {Until:o} — pausing at row {Row}.",
                    importJobId, until, i + 1);

                job.Status = ImportStatus.Paused;
                await importRepo.SaveChangesAsync(ct);
                scheduler.SchedulePartialFetch(importJobId, until - now); // re-enqueue self after cooldown
                return;
            }

            var row = rows[i];
            var rawRadicado = row.Radicado;
            var notes = row.Notes;

            // Normalize: strip every non-digit character.
            var normalized = rawRadicado is null ? string.Empty : NonDigit.Replace(rawRadicado, string.Empty);

            // Skip blank/header rows silently (blueprint: "se saltan en silencio").
            if (normalized.Length == 0) continue;

            if (normalized.Length != 23)
            {
                errors.Add(new ImportRowError(i + 2, rawRadicado ?? "", "INVALID_RADICADO",
                    "El radicado no tiene 23 dígitos. Cárgalo manualmente con el wizard."));
                job.ErrorCount++;
                await FlushProgress(job, i, ct);
                continue;
            }

            // Trickle delay between API calls (not before the very first).
            if (!first)
                await syncDelay.WaitAsync(PaceDelay(throttle), ct);
            first = false;

            // Import via the synchronous full-number flow (reuses the same logic as POST /processes).
            var result = await processCreator.CreateAsync(job.UserId, normalized, notes, ct);
            if (result.IsSuccess)
            {
                job.SuccessCount++;
            }
            else if (result.Error == "DUPLICATE_PROCESS")
            {
                // Already in their portfolio — skip silently (not a user error), but still
                // tracked so the ImportComplete email can report "N duplicados".
                job.DuplicateCount++;
                logger.LogInformation(
                    "BulkImportJob {Id}: row {Row} radicado {Radicado} already exists, skipping.",
                    importJobId, i + 2, normalized);
            }
            else if (result.Error == "RAMA_OVERVIEW_FAILED")
            {
                // Likely a WAF 403 inside ProcessCreator. Set cooldown and pause.
                var cooldownUntil = now.AddMinutes(waf.CooldownMinutesOnBlock);
                await syncState.SetWafBlockedUntilAsync(
                    cooldownUntil, $"BulkImport WAF at row {i + 2}", ct);

                job.Status = ImportStatus.Paused;
                await importRepo.SaveChangesAsync(ct);
                scheduler.SchedulePartialFetch(importJobId, TimeSpan.FromMinutes(waf.CooldownMinutesOnBlock));
                logger.LogWarning(
                    "BulkImportJob {Id}: WAF hit at row {Row}, pausing until {Until:o}.",
                    importJobId, i + 2, cooldownUntil);
                return;
            }
            else
            {
                errors.Add(new ImportRowError(i + 2, normalized, result.Error ?? "ERROR",
                    result.Error ?? "Error importando el proceso."));
                job.ErrorCount++;
            }

            await FlushProgress(job, i, ct);
        }

        // Finalize.
        job.Status = ImportStatus.Completed;
        job.CompletedAt = now;
        job.Errors = errors.Count > 0 ? JsonSerializer.Serialize(errors) : null;
        await importRepo.SaveChangesAsync(ct);

        // Insert outbox event for ImportComplete email (blueprint §9 step 7) and trigger
        // its dispatch — pass the OUTBOX row id, not the import job id: the payload already
        // carries everything DispatchImportCompleteJob needs to render/send.
        var outboxId = await InsertImportCompleteOutboxAsync(job, errors, now, ct);
        scheduler.EnqueueImportComplete(outboxId);

        logger.LogInformation(
            "BulkImportJob {Id} completed: success={S} errors={E}.",
            importJobId, job.SuccessCount, job.ErrorCount);
    }

    private async Task FlushProgress(ImportJob job, int rowIndex, CancellationToken ct)
    {
        job.ProcessedRows = rowIndex + 1;
        // Flush every 5 rows for responsive frontend polling.
        if (job.ProcessedRows % 5 == 0)
            await importRepo.SaveChangesAsync(ct);
    }

    private async Task FailJobAsync(ImportJob job, string reason, CancellationToken ct)
    {
        job.Status = ImportStatus.Failed;
        job.SyncError = reason;
        await importRepo.SaveChangesAsync(ct);
        logger.LogError("BulkImportJob {Id} failed: {Reason}.", job.Id, reason);
    }

    private async Task<Guid> InsertImportCompleteOutboxAsync(
        ImportJob job, IReadOnlyList<ImportRowError> errors, DateTimeOffset now, CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(new ImportCompleteOutboxPayload(
            ImportJobId: job.Id,
            FileName: job.FileName,
            TotalRows: job.TotalRows,
            SuccessCount: job.SuccessCount,
            DuplicateCount: job.DuplicateCount,
            ErrorCount: job.ErrorCount,
            CompletedAt: now,
            Errors: errors.Select(e => new ImportErrorRow(e.Row, e.Radicado, e.Code, e.Message)).ToList()));

        var outboxId = Guid.NewGuid();
        await outboxRepo.InsertAsync(new OutboxMessage
        {
            Id = outboxId,
            UserId = job.UserId,
            EventType = "ImportComplete",
            Channel = "email",
            Payload = payload,
            Status = "pending",
            CreatedAt = now,
        }, ct);

        return outboxId;
    }

    private static TimeSpan PaceDelay(ThrottleOptions opts)
    {
        var jitterMs = (opts.InitialFetchIntervalSecondsMax - opts.InitialFetchIntervalSecondsMin) * 1000;
        return TimeSpan.FromSeconds(opts.InitialFetchIntervalSecondsMin)
             + TimeSpan.FromMilliseconds(Random.Shared.Next(0, Math.Max(1, jitterMs)));
    }

    private static List<ImportRow>? DeserializePayload(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try { return JsonSerializer.Deserialize<List<ImportRow>>(json, PayloadJson); }
        catch { return null; }
    }
}

public sealed record ImportRowError(int Row, string Radicado, string Code, string Message);
