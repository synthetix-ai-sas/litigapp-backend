using System.Net;
using System.Text.Json;
using LitigApp.Application.Common.Abstractions;
using LitigApp.Infrastructure.ExternalApis.RamaJudicial.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LitigApp.Infrastructure.ExternalApis.RamaJudicial;

/// <summary>
/// HTTP client for the Rama Judicial API.
/// Implements WAF-aware throttling (trickle + jitter) and maps raw API responses
/// to Application-layer result types. All methods return RamaResult to avoid
/// exceptions for expected errors (404, WAF block, transient failures).
/// </summary>
internal sealed class RamaJudicialClient : IRamaJudicialClient
{
    // ── JSON ──────────────────────────────────────────────────────────────────
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    // ── User-Agent rotation (WAF evasion) ─────────────────────────────────────
    private static readonly string[] UserAgents =
    [
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:125.0) Gecko/20100101 Firefox/125.0",
    ];

    private readonly HttpClient _httpClient;
    private readonly RamaJudicialOptions _options;
    private readonly ILogger<RamaJudicialClient> _logger;

    // Serialized access — MaxConcurrentRequests=1 prevents parallel WAF triggers
    private readonly SemaphoreSlim _semaphore;

    // Rotating UA index — Interlocked keeps it thread-safe
    private int _uaIndex;

    public RamaJudicialClient(
        HttpClient httpClient,
        IOptions<RamaJudicialOptions> options,
        ILogger<RamaJudicialClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        _semaphore = new SemaphoreSlim(_options.MaxConcurrentRequests, _options.MaxConcurrentRequests);
    }

    // ── IRamaJudicialClient ───────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<RamaResult<OverviewData?>> GetOverviewByFileNumberAsync(
        string fileNumber, CancellationToken ct)
    {
        var trimmed = fileNumber?.Trim() ?? string.Empty;
        if (trimmed.Length != 23)
            return RamaResult<OverviewData?>.Fail(FailureKind.InvalidInput,
                $"File number must be exactly 23 digits, got: '{trimmed}'");

        await _semaphore.WaitAsync(ct);
        try
        {
            using var req = BuildGet(
                $"/api/v2/Procesos/Consulta/NumeroRadicacion?numero={trimmed}&SoloActivos=false&pagina=1");
            using var response = await _httpClient.SendAsync(req, ct);

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                _logger.LogWarning("Rama Judicial WAF block (403) — overview {FileNumber}", trimmed);
                return RamaResult<OverviewData?>.Fail(FailureKind.WafBlocked, "WAF 403 Forbidden");
            }

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                var (kind, msg) = ParseError(response.StatusCode, body);
                return RamaResult<OverviewData?>.Fail(kind, msg);
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            var overview = JsonSerializer.Deserialize<OverviewResponse>(json, JsonOptions);

            if (overview?.Procesos is null || overview.Procesos.Count == 0)
                return RamaResult<OverviewData?>.Ok(null);

            var p = overview.Procesos[0];
            return RamaResult<OverviewData?>.Ok(new OverviewData(
                ExternalProcessId: p.IdProceso,
                ExternalConnectionId: p.IdConexion,
                LlaveProceso: p.LlaveProceso,
                FechaUltimaActuacion: p.FechaUltimaActuacion,
                Despacho: p.Despacho,
                Departamento: p.Departamento,
                EsPrivado: p.EsPrivado));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Transient error on Rama Judicial overview {FileNumber}", trimmed);
            return RamaResult<OverviewData?>.Fail(FailureKind.Transient, ex.Message);
        }
        finally
        {
            await ThrottleAsync(ct);
            _semaphore.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<RamaResult<ProcessDetailData?>> GetDetailAsync(
        long externalProcessId, CancellationToken ct)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            using var req = BuildGet($"/api/v2/Proceso/Detalle/{externalProcessId}");
            using var response = await _httpClient.SendAsync(req, ct);

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                _logger.LogWarning("Rama Judicial WAF block (403) — detail {ProcessId}", externalProcessId);
                return RamaResult<ProcessDetailData?>.Fail(FailureKind.WafBlocked, "WAF 403 Forbidden");
            }

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                var (kind, msg) = ParseError(response.StatusCode, body);
                return RamaResult<ProcessDetailData?>.Fail(kind, msg);
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            var detail = JsonSerializer.Deserialize<ProcessDetailResponse>(json, JsonOptions);

            if (detail is null)
                return RamaResult<ProcessDetailData?>.Ok(null);

            return RamaResult<ProcessDetailData?>.Ok(new ProcessDetailData(
                CodDespachoCompleto: detail.CodDespachoCompleto,
                Despacho: detail.Despacho,
                ExternalConnectionId: detail.IdConexion,
                EsPrivado: detail.EsPrivado,
                FechaProceso: detail.FechaProceso,
                TipoProceso: detail.TipoProceso,
                ClaseProceso: detail.ClaseProceso,
                SubclaseProceso: detail.SubclaseProceso,
                Recurso: detail.Recurso,
                Ponente: detail.Ponente,
                ContenidoRadicacion: detail.ContenidoRadicacion));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Transient error on Rama Judicial detail {ProcessId}", externalProcessId);
            return RamaResult<ProcessDetailData?>.Fail(FailureKind.Transient, ex.Message);
        }
        finally
        {
            await ThrottleAsync(ct);
            _semaphore.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<RamaResult<List<SubjectData>>> GetSubjectsAsync(
        long externalProcessId, CancellationToken ct)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            using var req = BuildGet($"/api/v2/Proceso/Sujetos/{externalProcessId}?pagina=1");
            using var response = await _httpClient.SendAsync(req, ct);

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                _logger.LogWarning("Rama Judicial WAF block (403) — subjects {ProcessId}", externalProcessId);
                return RamaResult<List<SubjectData>>.Fail(FailureKind.WafBlocked, "WAF 403 Forbidden");
            }

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                var (kind, msg) = ParseError(response.StatusCode, body);
                return RamaResult<List<SubjectData>>.Fail(kind, msg);
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<SubjectsResponse>(json, JsonOptions);

            var subjects = (result?.Sujetos ?? [])
                .Select(s => new SubjectData(
                    ExternalSubjectId: s.IdRegSujeto,
                    TipoSujeto: s.TipoSujeto,
                    EsEmplazado: s.EsEmplazado,
                    Identificacion: s.Identificacion,
                    NombreRazonSocial: s.NombreRazonSocial))
                .ToList();

            return RamaResult<List<SubjectData>>.Ok(subjects);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Transient error on Rama Judicial subjects {ProcessId}", externalProcessId);
            return RamaResult<List<SubjectData>>.Fail(FailureKind.Transient, ex.Message);
        }
        finally
        {
            await ThrottleAsync(ct);
            _semaphore.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<RamaResult<List<ActionData>>> GetFirstPageActionsAsync(
        long externalProcessId, CancellationToken ct)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            using var req = BuildGet($"/api/v2/Proceso/Actuaciones/{externalProcessId}?pagina=1");
            using var response = await _httpClient.SendAsync(req, ct);

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                _logger.LogWarning("Rama Judicial WAF block (403) — actions {ProcessId}", externalProcessId);
                return RamaResult<List<ActionData>>.Fail(FailureKind.WafBlocked, "WAF 403 Forbidden");
            }

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);

                // Spike finding §5: 404 "No se encontraron Actuaciones" → empty list (not an error)
                if (response.StatusCode == HttpStatusCode.NotFound &&
                    body.Contains("No se encontraron Actuaciones", StringComparison.OrdinalIgnoreCase))
                {
                    return RamaResult<List<ActionData>>.Ok([]);
                }

                var (kind, msg) = ParseError(response.StatusCode, body);
                return RamaResult<List<ActionData>>.Fail(kind, msg);
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<ActionsResponse>(json, JsonOptions);

            var actions = (result?.Actuaciones ?? [])
                .Select(a => new ActionData(
                    ExternalActionId: a.IdRegActuacion,
                    ConsActuacion: a.ConsActuacion,
                    FechaActuacion: a.FechaActuacion,
                    Actuacion: a.Actuacion,
                    Anotacion: a.Anotacion,
                    FechaInicial: a.FechaInicial,
                    FechaFinal: a.FechaFinal,
                    FechaRegistro: a.FechaRegistro,
                    CodRegla: a.CodRegla?.Trim(), // Spike finding §4: trailing spaces
                    ConDocumentos: a.ConDocumentos))
                .ToList();

            return RamaResult<List<ActionData>>.Ok(actions);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Transient error on Rama Judicial actions {ProcessId}", externalProcessId);
            return RamaResult<List<ActionData>>.Fail(FailureKind.Transient, ex.Message);
        }
        finally
        {
            await ThrottleAsync(ct);
            _semaphore.Release();
        }
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private HttpRequestMessage BuildGet(string url)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        var ua = UserAgents[(Interlocked.Increment(ref _uaIndex) & int.MaxValue) % UserAgents.Length];
        req.Headers.TryAddWithoutValidation("User-Agent", ua);
        req.Headers.TryAddWithoutValidation("Accept", "application/json, text/plain, */*");
        req.Headers.TryAddWithoutValidation("Accept-Language", "es-ES,es;q=0.9");
        req.Headers.TryAddWithoutValidation("Origin", "https://consultaprocesos.ramajudicial.gov.co");
        req.Headers.TryAddWithoutValidation("Referer", "https://consultaprocesos.ramajudicial.gov.co/");
        return req;
    }

    /// <summary>
    /// Maps an error HTTP response to a FailureKind by parsing the response body.
    /// Critical: 404 can mean NotFound, Transient (DB timeout), or InvalidInput
    /// depending on the message body — see spike findings §5.
    /// </summary>
    private static (FailureKind Kind, string Message) ParseError(
        HttpStatusCode statusCode, string body)
    {
        string message;
        try
        {
            var error = JsonSerializer.Deserialize<ApiErrorResponse>(body, JsonOptions);
            message = error?.Message ?? error?.ExceptionMessage ?? body;
        }
        catch
        {
            message = body;
        }

        // 404 — disambiguation required (spike finding §5)
        if (statusCode == HttpStatusCode.NotFound)
        {
            if (message.Contains("Connection Timeout", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("Timeout Expired", StringComparison.OrdinalIgnoreCase))
                return (FailureKind.Transient, message);

            if (message.Contains("ha de contener", StringComparison.OrdinalIgnoreCase))
                return (FailureKind.InvalidInput, message);

            return (FailureKind.NotFound, message);
        }

        // 400 — usually invalid input
        if (statusCode == HttpStatusCode.BadRequest)
        {
            if (message.Contains("digitos", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("dígitos", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("parametro", StringComparison.OrdinalIgnoreCase))
                return (FailureKind.InvalidInput, message);

            return (FailureKind.Transient, message);
        }

        // 5xx or anything else → Transient
        return (FailureKind.Transient, message);
    }

    /// <summary>
    /// Delays between requests to prevent WAF rate limiting.
    /// Swallows OperationCanceledException so the finally block never masks the caller's exception.
    /// </summary>
    private async Task ThrottleAsync(CancellationToken ct)
    {
        var delayMs = _options.DelayBetweenRequestsMs + Random.Shared.Next(0, _options.DelayJitterMs);
        try
        {
            await Task.Delay(delayMs, ct);
        }
        catch (OperationCanceledException)
        {
            // Do not propagate — throttle abort is acceptable during shutdown
        }
    }
}
