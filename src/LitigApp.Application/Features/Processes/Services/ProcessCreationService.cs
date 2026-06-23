using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Processes.Dtos;
using LitigApp.Domain.Common;
using LitigApp.Domain.Processes;

namespace LitigApp.Application.Features.Processes.Services;

/// <summary>
/// Shared synchronous creation flow for both /full-number and /wizard.
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
    ICurrentUserService currentUser)
{
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
            foreach (var s in subjects)
            {
                process.Subjects.Add(new ProcessSubject
                {
                    Id = Guid.NewGuid(),
                    ProcessId = process.Id,
                    ExternalSubjectId = s.ExternalSubjectId,
                    SubjectType = s.SubjectType,
                    IsSummoned = s.IsServedByPublication,
                    Identification = s.Identification,
                    Name = s.FullName,
                    Source = "api",
                    CreatedAt = now,
                });
            }
        }
        else
        {
            missing.Add("sujetos");
        }

        var actionsResult = await rama.GetFirstPageActionsAsync(overview.ExternalProcessId, ct);
        if (actionsResult.IsSuccess && actionsResult.Value is { } actions)
        {
            foreach (var a in actions)
            {
                process.Actions.Add(new ProcessAction
                {
                    Id = Guid.NewGuid(),
                    ProcessId = process.Id,
                    ExternalActionId = a.ExternalActionId,
                    ConsecutiveNumber = a.ActionNumber,
                    ActionDate = ToDateOnly(a.ActionDate),
                    Action = a.ActionType,
                    Annotation = a.Note,
                    TermStartDate = ToDateOnly(a.StartDate),
                    TermEndDate = ToDateOnly(a.EndDate),
                    RecordedAt = ToDateOnly(a.RegistrationDate),
                    HasDocuments = a.HasDocuments,
                    RuleCode = a.RuleCode,
                    CreatedAt = now,
                });
            }

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
            process.SyncPhase = "pending_partial_completion";
            process.SyncError = $"No se pudo completar: {string.Join(", ", missing)}.";
        }
        else
        {
            process.SyncStatus = ProcessSyncStatus.Ok;
            process.SyncPhase = "idle";
            process.LastSyncedAt = now;
        }

        await repository.AddAsync(process, ct);
        await repository.SaveChangesAsync(ct);

        if (missing.Count > 0)
            partialFetchScheduler.SchedulePartialCompletion(process.Id);

        var dto = await reader.GetByIdAsync(userId, process.Id, ct);
        return Result<ProcessDetailDto>.Success(dto!);
    }

    private static DateTimeOffset? ToUtc(DateTime? value) =>
        value is null ? null : new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc));

    private static DateOnly? ToDateOnly(DateTime? value) =>
        value is null ? null : DateOnly.FromDateTime(value.Value);
}
