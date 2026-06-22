namespace LitigApp.Application.Common.Abstractions;

/// <summary>
/// Access to global sync-engine state stored in the key-value <c>sync_state</c> table:
/// WAF cooldown window and the adaptive throttle (seconds between API calls).
/// Infrastructure implements this; jobs (OverviewSweep/ActionsSweep) consume it.
/// </summary>
public interface ISyncStateService
{
    /// <summary>
    /// Current WAF cooldown deadline, or null if not blocked. While now &lt; this value,
    /// sweep jobs must skip their run (see blueprint §10.2).
    /// </summary>
    Task<DateTimeOffset?> GetWafBlockedUntilAsync(CancellationToken ct);

    /// <summary>
    /// Sets the WAF cooldown deadline after a 403, with a human-readable reason for audit.
    /// </summary>
    Task SetWafBlockedUntilAsync(DateTimeOffset until, string reason, CancellationToken ct);

    /// <summary>Current overview-sweep throttle (seconds between API calls). Defaults to 3 if unset.</summary>
    Task<int> GetOverviewThrottleSecondsAsync(CancellationToken ct);

    /// <summary>Persists a new overview-sweep throttle value (adaptive throttling, PR4).</summary>
    Task SetOverviewThrottleSecondsAsync(int seconds, CancellationToken ct);

    /// <summary>Current actions-sweep throttle (seconds between API calls). Defaults to 3 if unset.</summary>
    Task<int> GetActionsThrottleSecondsAsync(CancellationToken ct);
}
