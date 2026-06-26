using LitigApp.Application.Common.Abstractions;
using LitigApp.Domain.Processes;

namespace LitigApp.Application.Features.Processes.Sync;

/// <summary>
/// Maps a Rama Judicial <see cref="SubjectData"/> to a <see cref="ProcessSubject"/> row.
/// Shared by the synchronous creation flow and CompletePartialFetchJob.
/// </summary>
public static class ProcessSubjectFactory
{
    public static ProcessSubject Create(SubjectData s, Guid processId, DateTimeOffset createdAt) => new()
    {
        Id = Guid.NewGuid(),
        ProcessId = processId,
        ExternalSubjectId = s.ExternalSubjectId,
        SubjectType = s.SubjectType,
        IsSummoned = s.IsServedByPublication,
        Identification = s.Identification,
        Name = s.FullName,
        Source = "api",
        CreatedAt = createdAt,
    };
}
