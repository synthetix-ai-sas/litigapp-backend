using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Processes.Dtos;
using LitigApp.Application.Features.Processes.Sync;
using LitigApp.Domain.Common;
using LitigApp.Domain.Processes;

namespace LitigApp.Application.Features.Processes.Services;

/// <summary>
/// Shared synchronous creation flow for both /full-number, /wizard, and BulkImportJob.
/// Calls the Rama Judicial API (overview → detail → subjects → actions), persists the
/// process graph in a single SaveChanges, and degrades to a "partial" status if any
/// post-overview call fails. Returns the created process detail.
/// </summary>
public sealed class ProcessCreationService(
    IProcessRepository repository,
    IRamaJudicialClient rama,
    IProcessReader reader,
    IDateTimeProvider clock,
    IPartialFetchScheduler partialFetchScheduler,
    ICurrentUserService currentUser) : IProcessCreator
{
    /// <summary>HTTP-context entry point: pulls userId from <see cref="ICurrentUserService"/>
    /// and checks for an active import (bloqueo mutuo — blueprint §9).</summary>
    public async Task<Result<ProcessDetailDto>> CreateAsync(
        string fileNumber, string? alias, CancellationToken ct)
    {
        fileNumber = (fileNumber ?? string.Empty).Trim();
        if (!FileNumberRules.IsValid(fileNumber))
            return Result<ProcessDetailDto>.Failure(ProcessErrorCodes.InvalidFileNumber);

        var userId = currentUser.UserId!;

        if (await repository.HasActiveImportAsync(userId, ct))
            return Result<ProcessDetailDto>.Failure(ProcessErrorCodes.ImportInProgress);

        if (await repository.ExistsAsync(userId, fileNumber, ct))
            return Result<ProcessDetailDto>.Failure(ProcessErrorCodes.DuplicateProcess);

        return await CoreCreateAsync(userId, fileNumber, alias, ct);
    }

    private async Task<Result<ProcessDetailDto>> CoreCreateAsync(
        string userId, string fileNumber, string? alias, CancellationToken ct)
    {
        var overviewResult = await rama.GetOverviewByFileNumberAsync(fileNumber, ct);
        if (!overviewResult.IsSuccess)
            return Result<ProcessDetailDto>.Failure(ProcessErrorCodes.RamaOverviewFailed);
        if (overviewResult.Value is not { } overview)
            return Result<ProcessDetailDto>.Failure(ProcessErrorCodes.ProcessNotFoundInRama);

        var now = clock.UtcNow;
        var court = await repository.FindCourtByOfficialCodeAsync(fileNumber[..12], ct);

        var process = new Process
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FileNumber = fileNumber,
            ExternalProcessId = overview.ExternalProcessId,
            ExternalConnectionId = overview.ExternalConnectionId,
            CourtId = court?.Id,
            FilingYear = short.Parse(fileNumber.Substring(12, 4)),
            IsPrivate = overview.IsPrivate,
            CustomAlias = string.IsNullOrWhiteSpace(alias) ? null : alias.Trim(),
            LastCourtActionAt = ToUtc(overview.LastActionDate),
            Attended = true,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };

        // ⚠️ Private process (blueprint "Manejo de procesos privados"): the overview returns
        // esPrivado=true and the other 3 endpoints (detail/subjects/actions) all respond 404.
        // Persist ONLY the overview — no further calls, no process_subjects/process_actions rows.
        // This is a normal, terminal state (sync_status='ok', idle), not a partial fetch.
        if (overview.IsPrivate)
        {
            process.SyncStatus = ProcessSyncStatus.Ok;
            process.SyncPhase = ProcessSyncPhase.Idle;
            process.LastSyncedAt = now;

            await repository.AddAsync(process, ct);
            await repository.SaveChangesAsync(ct);

            var privateDto = await reader.GetByIdAsync(userId, process.Id, ct);
            return Result<ProcessDetailDto>.Success(privateDto!);
        }

        var missing = new List<string>();

        var detailResult = await rama.GetDetailAsync(overview.ExternalProcessId, ct);
        if (detailResult.IsSuccess && detailResult.Value is { } detail)
        {
            process.ProcessType = detail.ProcessType;
            process.ProcessClass = detail.ProcessClass;
            process.ProcessSubclass = detail.ProcessSubclass;
            process.Resource = detail.Resource;
            process.JudgeName = detail.Rapporteur;
            process.FilingContent = detail.FilingContent;
        }
        else
        {
            missing.Add("detalle");
        }

        var subjectsResult = await rama.GetSubjectsAsync(overview.ExternalProcessId, ct);
        if (subjectsResult.IsSuccess && subjectsResult.Value is { } subjects)
        {
            // Defense: never insert a subject with a null/blank subject_type — the column is
            // NOT NULL by design. A malformed entry (e.g. a "PROCESO PRIVADO" placeholder) is
            // discarded rather than crashing the whole SaveChanges. (Blueprint §creación.)
            foreach (var s in subjects.Where(s => !string.IsNullOrWhiteSpace(s.SubjectType)))
                process.Subjects.Add(ProcessSubjectFactory.Create(s, process.Id, now));
        }
        else
        {
            missing.Add("sujetos");
        }

        var actionsResult = await rama.GetFirstPageActionsAsync(overview.ExternalProcessId, ct);
        if (actionsResult.IsSuccess && actionsResult.Value is { } actions)
        {
            foreach (var a in actions)
                process.Actions.Add(ProcessActionFactory.Create(a, process.Id, now));

            if (actions.Count > 0)
            {
                process.LastExternalConsecutive = actions.Max(a => a.ActionNumber);
                process.CurrentStatus = actions.OrderByDescending(a => a.ActionNumber).First().ActionType;
            }
        }
        else
        {
            missing.Add("actuaciones");
        }

        if (missing.Count > 0)
        {
            process.SyncStatus = ProcessSyncStatus.Partial;
            process.SyncPhase = ProcessSyncPhase.PendingPartialCompletion;
            process.SyncError = $"No se pudo completar: {string.Join(", ", missing)}.";
        }
        else
        {
            process.SyncStatus = ProcessSyncStatus.Ok;
            process.SyncPhase = ProcessSyncPhase.Idle;
            process.LastSyncedAt = now;
        }

        await repository.AddAsync(process, ct);
        await repository.SaveChangesAsync(ct);

        if (missing.Count > 0)
            partialFetchScheduler.SchedulePartialCompletion(process.Id);

        var dto = await reader.GetByIdAsync(userId, process.Id, ct);
        return Result<ProcessDetailDto>.Success(dto!);
    }

    /// <summary>
    /// Job-context entry point (IProcessCreator): userId provided directly.
    /// Skips the active-import check (the caller IS the import job).
    /// Used by BulkImportJob per row.
    /// </summary>
    async Task<Result<ProcessDetailDto>> IProcessCreator.CreateAsync(
        string userId, string fileNumber, string? alias, CancellationToken ct)
    {
        fileNumber = (fileNumber ?? string.Empty).Trim();
        if (!FileNumberRules.IsValid(fileNumber))
            return Result<ProcessDetailDto>.Failure(ProcessErrorCodes.InvalidFileNumber);

        if (await repository.ExistsAsync(userId, fileNumber, ct))
            return Result<ProcessDetailDto>.Failure(ProcessErrorCodes.DuplicateProcess);

        return await CoreCreateAsync(userId, fileNumber, alias, ct);
    }

    private static DateTimeOffset? ToUtc(DateTime? value) =>
        value is null ? null : new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc));
}
