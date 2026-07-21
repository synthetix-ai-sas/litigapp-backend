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

            if (overview?.Processes is null || overview.Processes.Count == 0)
                return RamaResult<OverviewData?>.Ok(null);

            var p = overview.Processes[0];
            return RamaResult<OverviewData?>.Ok(new OverviewData(
                ExternalProcessId: p.ProcessId,
                ExternalConnectionId: p.ConnectionId,
                ProcessKey: p.ProcessKey,
                LastActionDate: p.LastActionDate,
                CourtName: p.CourtName,
                Department: p.Department,
                IsPrivate: p.IsPrivate));
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
                CourtFullCode: detail.CourtFullCode,
                CourtName: detail.CourtName,
                ExternalConnectionId: detail.ConnectionId,
                IsPrivate: detail.IsPrivate,
                ProcessDate: detail.ProcessDate,
                ProcessType: detail.ProcessType,
                ProcessClass: detail.ProcessClass,
                ProcessSubclass: detail.ProcessSubclass,
                Resource: detail.Resource,
                Rapporteur: detail.Rapporteur,
                FilingContent: detail.FilingContent));
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

            var subjects = (result?.Subjects ?? [])
                .Select(s => new SubjectData(
                    ExternalSubjectId: s.SubjectId,
                    SubjectType: s.SubjectType,
                    IsServedByPublication: s.IsServedByPublication,
                    Identification: s.Identification,
                    FullName: s.FullName))
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

            var actions = (result?.Actions ?? [])
                .Select(a => new ActionData(
                    ExternalActionId: a.ActionId,
                    ActionNumber: a.ActionNumber,
                    ActionDate: a.ActionDate,
                    ActionType: a.ActionType,
                    Note: a.Note,
                    StartDate: a.StartDate,
                    EndDate: a.EndDate,
                    RegistrationDate: a.RegistrationDate,
                    RuleCode: a.RuleCode?.Trim(), // Spike finding §4: trailing spaces
                    HasDocuments: a.HasDocuments))
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

        // UA rotation from the config pool (WAF evasion)
        var pool = _options.UserAgentPool;
        if (pool.Length > 0)
        {
            var ua = pool[(Interlocked.Increment(ref _uaIndex) & int.MaxValue) % pool.Length];
            req.Headers.TryAddWithoutValidation("User-Agent", ua);
        }

        req.Headers.TryAddWithoutValidation("Accept", "application/json, text/plain, */*");
        req.Headers.TryAddWithoutValidation("Accept-Language", "es-ES,es;q=0.9");
        req.Headers.TryAddWithoutValidation("Origin", _options.Headers.Origin);
        req.Headers.TryAddWithoutValidation("Referer", _options.Headers.Referer);
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

            // "No se puede ver el detalle de un proceso privado" — private process (blueprint
            // "Manejo de procesos privados"). Terminal & known: never retry, never mark error.
            if (message.Contains("privado", StringComparison.OrdinalIgnoreCase))
                return (FailureKind.Private, message);

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
