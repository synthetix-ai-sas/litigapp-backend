using System.Text.Json.Serialization;

namespace LitigApp.Infrastructure.ExternalApis.RamaJudicial.Dtos;

internal sealed class ProcessDetailResponse
{
    // Spike finding §3: idRegProceso (8-digit) differs from idProceso (9-digit) used as external_process_id.
    // We do NOT persist this field — only idProceso from overview is stored.
    [JsonPropertyName("idRegProceso")]
    public long InternalProcessId { get; init; }

    [JsonPropertyName("llaveProceso")]
    public string ProcessKey { get; init; } = "";

    [JsonPropertyName("idConexion")]
    public int ConnectionId { get; init; }

    [JsonPropertyName("esPrivado")]
    public bool IsPrivate { get; init; }

    [JsonPropertyName("fechaProceso")]
    public DateTime? ProcessDate { get; init; }

    [JsonPropertyName("codDespachoCompleto")]
    public string CourtFullCode { get; init; } = "";

    [JsonPropertyName("despacho")]
    public string CourtName { get; init; } = "";

    [JsonPropertyName("ponente")]
    public string? Rapporteur { get; init; }

    [JsonPropertyName("tipoProceso")]
    public string? ProcessType { get; init; }

    [JsonPropertyName("claseProceso")]
    public string? ProcessClass { get; init; }

    [JsonPropertyName("subclaseProceso")]
    public string? ProcessSubclass { get; init; }

    [JsonPropertyName("recurso")]
    public string? Resource { get; init; }

    [JsonPropertyName("contenidoRadicacion")]
    public string? FilingContent { get; init; }
}
