namespace LitigApp.Application.Common.Abstractions;

// ──────────────────────────────────────────────────────────────────────────────
// Failure discrimination
// ──────────────────────────────────────────────────────────────────────────────

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

/// <summary>Structured failure returned by IRamaJudicialClient methods.</summary>
public sealed record RamaJudicialFailure(FailureKind Kind, string Message);

// ──────────────────────────────────────────────────────────────────────────────
// Application-level result models (returned by the interface)
// These are the "clean" types that Application / Jobs consume.
// Infrastructure maps raw API DTOs → these types before returning.
// ──────────────────────────────────────────────────────────────────────────────

public sealed record OverviewData(
    long ExternalProcessId,
    int ExternalConnectionId,
    string LlaveProceso,
    DateTime? FechaUltimaActuacion,
    string Despacho,
    string Departamento,
    bool EsPrivado);

public sealed record ProcessDetailData(
    string CodDespachoCompleto,
    string Despacho,
    int ExternalConnectionId,
    bool EsPrivado,
    DateTime? FechaProceso,
    string? TipoProceso,
    string? ClaseProceso,
    string? SubclaseProceso,
    string? Recurso,
    string? Ponente,
    string? ContenidoRadicacion);

public sealed record SubjectData(
    long ExternalSubjectId,
    string TipoSujeto,
    bool EsEmplazado,
    string? Identificacion,
    string NombreRazonSocial);

public sealed record ActionData(
    long ExternalActionId,
    int ConsActuacion,
    DateTime? FechaActuacion,
    string Actuacion,
    string? Anotacion,
    DateTime? FechaInicial,
    DateTime? FechaFinal,
    DateTime? FechaRegistro,
    string? CodRegla,
    bool ConDocumentos);

// ──────────────────────────────────────────────────────────────────────────────
// Result wrapper — keeps FailureKind typed, avoids exceptions for expected errors
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Discriminated union: either a successful value T or a RamaJudicialFailure.
/// </summary>
public sealed class RamaResult<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public RamaJudicialFailure? Failure { get; }

    private RamaResult(bool isSuccess, T? value, RamaJudicialFailure? failure)
    {
        IsSuccess = isSuccess;
        Value = value;
        Failure = failure;
    }

    public static RamaResult<T> Ok(T value) => new(true, value, null);
    public static RamaResult<T> Fail(RamaJudicialFailure failure) => new(false, default, failure);

    // Convenience: static factory via FailureKind
    public static RamaResult<T> Fail(FailureKind kind, string message) =>
        Fail(new RamaJudicialFailure(kind, message));
}

// ──────────────────────────────────────────────────────────────────────────────
// Interface
// ──────────────────────────────────────────────────────────────────────────────

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
    /// Returns Ok(null) when the process is not found (200 with empty procesos[]).
    /// Returns Fail(NotFound) when the API confirms the process doesn't exist.
    /// Returns Fail(Transient) when the API has DB issues (400/404 with SQL timeout message).
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
