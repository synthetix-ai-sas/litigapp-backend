namespace LitigApp.Domain.Common;

/// <summary>
/// Key-value store for global sync engine state (WAF cooldown, throttle settings).
/// </summary>
public class SyncState
{
    public string Key { get; set; } = string.Empty;
    public string? ValueText { get; set; }
    public DateTimeOffset? ValueTimestamp { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
