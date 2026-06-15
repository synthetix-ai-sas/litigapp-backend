using System.ComponentModel.DataAnnotations;

namespace LitigApp.Jobs;

public sealed class SweepOptions
{
    public const string SectionName = "RamaJudicial:Sweep";

    [Range(1, 1440)]
    public int OverviewIntervalMinutes { get; init; } = 15;

    [Range(1, 500)]
    public int BatchSize { get; init; } = 50;

    [Range(1, 48)]
    public int MinimumHoursBetweenSyncsPerProcess { get; init; } = 22;
}
