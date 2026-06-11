namespace LitigApp.Application.Common.Abstractions;

public sealed record OverviewData(
    long ExternalProcessId,
    int ExternalConnectionId,
    string ProcessKey,
    DateTime? LastActionDate,
    string CourtName,
    string Department,
    bool IsPrivate);
