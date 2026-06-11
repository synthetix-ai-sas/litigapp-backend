namespace LitigApp.Application.Common.Abstractions;

public sealed record ProcessDetailData(
    string CourtFullCode,
    string CourtName,
    int ExternalConnectionId,
    bool IsPrivate,
    DateTime? ProcessDate,
    string? ProcessType,
    string? ProcessClass,
    string? ProcessSubclass,
    string? Resource,
    string? Rapporteur,
    string? FilingContent);
