using System.Text.Json;
using System.Text.RegularExpressions;
using Hangfire;
using LitigApp.Application.Common.Abstractions;
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
    IImportPreviewCache previewCache,
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

        // If the job was re-enqueued after a pause, the preview may have expired (10-min TTL).
        // In that case we cannot continue — mark as failed.
        var mapping = DeserializeMapping(job.ColumnMapping);
        if (mapping is null)
        {
            await FailJobAsync(job, "column_mapping missing or invalid", ct);
            return;
        }

        var preview = previewCache.Get(job.PreviewId);
        if (preview is null)
        {
            await FailJobAsync(job, "preview_expired", ct);
            return;
        }

        job.Status = ImportStatus.Running;
        job.TotalRows = preview.Rows.Count;
        await importRepo.SaveChangesAsync(ct);

        var errors = new List<ImportRowError>();
        var first = true;

        for (var i = 0; i < preview.Rows.Count; i++)
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

            var row = preview.Rows[i];
            var rawRadicado = row.TryGetValue(mapping.RadicadoCol, out var rv) ? rv : null;
            var notes = mapping.NotesCol is not null && row.TryGetValue(mapping.NotesCol, out var nv) ? nv : null;

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
                // Already in their portfolio — skip silently (not a user error).
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

        // Insert outbox event for ImportComplete email (blueprint §9 step 7).
        await InsertImportCompleteOutboxAsync(job, now, ct);

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

    private async Task InsertImportCompleteOutboxAsync(
        ImportJob job, DateTimeOffset now, CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(new
        {
            importJobId = job.Id,
            fileName = job.FileName,
            totalRows = job.TotalRows,
            successCount = job.SuccessCount,
            errorCount = job.ErrorCount,
            completedAt = now,
        });

        await outboxRepo.InsertAsync(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            UserId = job.UserId,
            EventType = "ImportComplete",
            Channel = "email",
            Payload = payload,
            Status = "pending",
            CreatedAt = now,
        }, ct);
    }

    private static TimeSpan PaceDelay(ThrottleOptions opts)
    {
        var jitterMs = (opts.InitialFetchIntervalSecondsMax - opts.InitialFetchIntervalSecondsMin) * 1000;
        return TimeSpan.FromSeconds(opts.InitialFetchIntervalSecondsMin)
             + TimeSpan.FromMilliseconds(Random.Shared.Next(0, Math.Max(1, jitterMs)));
    }

    private static ColumnMapping? DeserializeMapping(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try { return JsonSerializer.Deserialize<ColumnMapping>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); }
        catch { return null; }
    }

    private sealed record ColumnMapping(string RadicadoCol, string? NotesCol);
}

public sealed record ImportRowError(int Row, string Radicado, string Code, string Message);
