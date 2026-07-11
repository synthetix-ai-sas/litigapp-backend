namespace LitigApp.Application.Features.Processes.Dtos;

public sealed record CourtSummaryDto(
    Guid Id,
    string Name,
    string? CityName,
    string? DepartmentName);

public sealed record ProcessSubjectDto(
    string Type,
    string Name,
    string? Identification,
    bool IsSummoned);

public sealed record ProcessActionDto(
    Guid Id,
    int ConsecutiveNumber,
    DateOnly? ActionDate,
    string? Action,
    string? Annotation,
    DateOnly? TermStartDate,
    DateOnly? TermEndDate,
    Guid? GroupedWithId);

/// <summary>Full detail of a process, including subjects and actions.</summary>
public sealed record ProcessDetailDto(
    Guid Id,
    string FileNumber,
    string? Alias,
    CourtSummaryDto? Court,
    short? FilingYear,
    string? ProcessType,
    string? ProcessClass,
    string? JudgeName,
    string? CurrentStatus,
    DateTimeOffset? LastCourtActionAt,
    bool Attended,
    string SyncStatus,
    string SyncPhase,
    bool IsPrivate,
    bool CanDownloadPdf,
    IReadOnlyList<ProcessSubjectDto> Subjects,
    IReadOnlyList<ProcessActionDto> Actions);
