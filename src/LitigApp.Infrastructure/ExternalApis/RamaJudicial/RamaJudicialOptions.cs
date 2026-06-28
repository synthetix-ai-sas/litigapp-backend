namespace LitigApp.Infrastructure.ExternalApis.RamaJudicial;

/// <summary>
/// Configuration for the Rama Judicial HTTP client.
/// Bound from appsettings.json section "RamaJudicial".
/// </summary>
public sealed class RamaJudicialOptions
{
    public const string SectionName = "RamaJudicial";

    /// <summary>Base URL of the Rama Judicial API.</summary>
    public string BaseUrl { get; init; } = "https://consultaprocesos.ramajudicial.gov.co:448";

    /// <summary>
    /// Per-attempt HTTP timeout in seconds. Spike found overview can take up to 35s
    /// when the API DB is under stress — set to 30 by default (spike-adjusted from 15).
    /// </summary>
    public int TimeoutSeconds { get; init; } = 30;

    /// <summary>
    /// Minimum safety floor between API requests (ms). The sweep jobs own the main adaptive
    /// pacing (decision D1, throttle read from sync_state); this floor protects ad-hoc callers
    /// (synchronous creation, bulk import) regardless of the job throttle.
    /// </summary>
    public int DelayBetweenRequestsMs { get; init; } = 500;

    /// <summary>Random jitter added to DelayBetweenRequestsMs (ms).</summary>
    public int DelayJitterMs { get; init; } = 500;

    /// <summary>Maximum number of concurrent requests to the API (WAF protection).</summary>
    public int MaxConcurrentRequests { get; init; } = 1;
}
