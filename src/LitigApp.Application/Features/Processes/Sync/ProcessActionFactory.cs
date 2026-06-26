using LitigApp.Application.Common.Abstractions;
using LitigApp.Domain.Processes;

namespace LitigApp.Application.Features.Processes.Sync;

/// <summary>
/// Maps a Rama Judicial <see cref="ActionData"/> to a <see cref="ProcessAction"/> row.
/// Shared by the synchronous creation flow and the ActionsSweep job so the mapping
/// lives in exactly one place.
/// </summary>
public static class ProcessActionFactory
{
    public static ProcessAction Create(ActionData a, Guid processId, DateTimeOffset createdAt) => new()
    {
        Id = Guid.NewGuid(),
        ProcessId = processId,
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
        CreatedAt = createdAt,
    };

    private static DateOnly? ToDateOnly(DateTime? value) =>
        value is null ? null : DateOnly.FromDateTime(value.Value);
}
