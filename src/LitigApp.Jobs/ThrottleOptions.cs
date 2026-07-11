using System.ComponentModel.DataAnnotations;

namespace LitigApp.Jobs;

/// <summary>
/// Per-call trickle delays applied by the sweep jobs (D1: pacing lives in the job, not the
/// HTTP client). Min/Max define a jitter range in seconds. Bound from "RamaJudicial:Throttle".
/// </summary>
public sealed class ThrottleOptions
{
    public const string SectionName = "RamaJudicial:Throttle";

    [Range(1, 60)]
    public int OverviewIntervalSecondsMin { get; init; } = 2;

    [Range(1, 60)]
    public int OverviewIntervalSecondsMax { get; init; } = 3;

    [Range(1, 60)]
    public int ActionsIntervalSecondsMin { get; init; } = 2;

    [Range(1, 60)]
    public int ActionsIntervalSecondsMax { get; init; } = 3;

    /// <summary>
    /// Delay applied between rows in BulkImportJob (blueprint §6.1 InitialFetchInterval).
    /// Shorter than sweep intervals because the user is waiting synchronously.
    /// </summary>
    [Range(1, 30)]
    public int InitialFetchIntervalSecondsMin { get; init; } = 1;

    [Range(1, 30)]
    public int InitialFetchIntervalSecondsMax { get; init; } = 2;
}
