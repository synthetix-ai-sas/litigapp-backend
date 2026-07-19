namespace LitigApp.Application.Features.Notifications.Dtos;

/// <summary>
/// A process changed since the user's last digest — the source row for the "Últimas
/// actuaciones detectadas" table (UserDigestTemplate.html). Produced by
/// <see cref="Common.Abstractions.IProcessRepository.GetChangedSinceAsync"/>, already
/// ordered by <see cref="LastCourtActionAt"/> descending.
/// </summary>
public sealed record ChangedProcessDto(
    Guid ProcessId,
    string FileNumber,
    DateTimeOffset LastCourtActionAt,
    string? LatestAction,
    string? LatestAnnotation);
