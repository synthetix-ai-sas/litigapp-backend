using System.ComponentModel.DataAnnotations;

namespace LitigApp.Jobs;

/// <summary>
/// WAF resilience tunables (blueprint §6.1 "Waf"). Bound from "RamaJudicial:Waf".
/// Consumed by the sweep jobs for cooldown and adaptive throttling (PR2/PR4).
/// </summary>
public sealed class WafOptions
{
    public const string SectionName = "RamaJudicial:Waf";

    /// <summary>How long to wait after a 403 before resuming sweeps.</summary>
    [Range(1, 240)]
    public int CooldownMinutesOnBlock { get; init; } = 20;

    /// <summary>Consecutive successes before lowering the throttle (speeding up).</summary>
    [Range(1, 100000)]
    public int ConsecutiveSuccessesToSpeedUp { get; init; } = 100;

    /// <summary>Consecutive blocks before raising the throttle (slowing down).</summary>
    [Range(1, 1000)]
    public int ConsecutiveBlocksToSlowDown { get; init; } = 1;

    /// <summary>Ceiling for the adaptive throttle when things go wrong.</summary>
    [Range(2, 120)]
    public int EmergencyMaxThrottleSeconds { get; init; } = 10;
}
