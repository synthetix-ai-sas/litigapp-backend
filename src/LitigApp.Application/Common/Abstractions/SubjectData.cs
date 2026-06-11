namespace LitigApp.Application.Common.Abstractions;

public sealed record SubjectData(
    long ExternalSubjectId,
    string SubjectType,
    bool IsServedByPublication,
    string? Identification,
    string FullName);
