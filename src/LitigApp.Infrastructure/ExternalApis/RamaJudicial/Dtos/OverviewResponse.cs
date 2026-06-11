using System.Text.Json.Serialization;

namespace LitigApp.Infrastructure.ExternalApis.RamaJudicial.Dtos;

internal sealed class OverviewResponse
{
    [JsonPropertyName("tipoConsulta")]
    public string QueryType { get; init; } = "";

    [JsonPropertyName("procesos")]
    public List<OverviewProcess> Processes { get; init; } = [];

    [JsonPropertyName("paginacion")]
    public Pagination Pagination { get; init; } = new();
}

internal sealed class OverviewProcess
{
    [JsonPropertyName("idProceso")]
    public long ProcessId { get; init; }

    [JsonPropertyName("idConexion")]
    public int ConnectionId { get; init; }

    [JsonPropertyName("llaveProceso")]
    public string ProcessKey { get; init; } = "";

    [JsonPropertyName("fechaUltimaActuacion")]
    public DateTime? LastActionDate { get; init; }

    [JsonPropertyName("despacho")]
    public string CourtName { get; init; } = "";

    [JsonPropertyName("departamento")]
    public string Department { get; init; } = "";

    [JsonPropertyName("esPrivado")]
    public bool IsPrivate { get; init; }

    // Spike finding §4: extra field present in real API, always -1, ignored
    [JsonPropertyName("cantFilas")]
    public int RowCount { get; init; }
}
