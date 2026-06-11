namespace LitigApp.Application.Common.Abstractions;

public sealed record ActionData(
    long ExternalActionId,
    int ActionNumber,
    DateTime? ActionDate,
    string ActionType,
    string? Note,
    DateTime? StartDate,
    DateTime? EndDate,
    DateTime? RegistrationDate,
    string? RuleCode,
    bool HasDocuments);
