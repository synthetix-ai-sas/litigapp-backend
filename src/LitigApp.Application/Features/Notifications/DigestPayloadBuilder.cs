using LitigApp.Application.Features.Notifications.Dtos;

namespace LitigApp.Application.Features.Notifications;

/// <summary>Result of cutting a changed-process list down to the digest's visible rows.</summary>
public readonly record struct DigestCut(IReadOnlyList<ChangedProcessDto> Shown, int Remaining, int Total);

/// <summary>
/// Applies the digest row limit (blueprint §10.4): show the first
/// <c>Notifications:DigestMaxRows</c> processes (caller must have already ordered them by
/// most-recent action first) and summarize the rest as "y N procesos más".
/// </summary>
public static class DigestPayloadBuilder
{
    public static DigestCut Build(IReadOnlyList<ChangedProcessDto> changed, int maxRows)
    {
        var shown = changed.Count <= maxRows ? changed : changed.Take(maxRows).ToList();
        return new DigestCut(shown, changed.Count - shown.Count, changed.Count);
    }
}
