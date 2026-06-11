namespace LitigApp.Application.Common.Abstractions;

/// <summary>
/// Contract for querying the Rama Judicial API. Infrastructure implements this.
/// All methods return RamaResult to force callers to handle failures explicitly.
/// WAF blocked (403) is surfaced as FailureKind.WafBlocked — caller MUST stop
/// and set waf_blocked_until in sync_state.
/// </summary>
public interface IRamaJudicialClient
{
    /// <summary>
    /// Overview query by 23-digit radicado.
    /// Returns Ok(null) when the process list is empty (radicado not found in system).
    /// Returns Fail(Transient) when the API has DB issues (404 with SQL timeout body).
    /// Returns Fail(InvalidInput) for malformed radicados (not 23 digits).
    /// </summary>
    Task<RamaResult<OverviewData?>> GetOverviewByFileNumberAsync(string fileNumber, CancellationToken ct);

    /// <summary>
    /// Process detail by internal process ID (idProceso from overview, 9-digit long).
    /// Returns Ok(null) when not found.
    /// </summary>
    Task<RamaResult<ProcessDetailData?>> GetDetailAsync(long externalProcessId, CancellationToken ct);

    /// <summary>
    /// Page 1 of subjects for the process. Returns Ok([]) when no subjects.
    /// </summary>
    Task<RamaResult<List<SubjectData>>> GetSubjectsAsync(long externalProcessId, CancellationToken ct);

    /// <summary>
    /// Page 1 of actions (most recent ~40 out of N total). Returns Ok([]) when none.
    /// MVP: only page 1 is fetched. Full pagination is a Tier 2 enhancement.
    /// </summary>
    Task<RamaResult<List<ActionData>>> GetFirstPageActionsAsync(long externalProcessId, CancellationToken ct);
}
