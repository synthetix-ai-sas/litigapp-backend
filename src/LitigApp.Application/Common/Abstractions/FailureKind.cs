namespace LitigApp.Application.Common.Abstractions;

/// <summary>
/// Distinguishes the type of failure returned by IRamaJudicialClient.
/// The sync engine uses this to decide: cooldown (WafBlocked), skip (NotFound/InvalidInput),
/// or retry later (Transient).
/// </summary>
public enum FailureKind
{
    None,
    /// <summary>Process does not exist in Rama Judicial (definitive, do not retry).</summary>
    NotFound,
    /// <summary>403 Forbidden — WAF blocked. Jobs must set waf_blocked_until and stop.</summary>
    WafBlocked,
    /// <summary>5xx, timeout, or transient DB error on the API server. Polly already retried; caller may retry later.</summary>
    Transient,
    /// <summary>400 or 404 due to malformed input (e.g. radicado != 23 digits). Do not retry.</summary>
    InvalidInput
}
