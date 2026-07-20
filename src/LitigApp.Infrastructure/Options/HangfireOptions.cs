using System.ComponentModel.DataAnnotations;

namespace LitigApp.Infrastructure.Options;

/// <summary>Hangfire server and dashboard settings. Bound from section "Hangfire".</summary>
public sealed class HangfireOptions
{
    public const string SectionName = "Hangfire";

    /// <summary>Basic Auth password for /hangfire outside Development. Null/empty denies access.</summary>
    public string? DashboardPassword { get; init; }

    /// <summary>Number of Hangfire worker threads (worker role only). Defaults to ProcessorCount×2 if not set.</summary>
    [Range(1, 50)]
    public int? WorkerCount { get; init; }
}
