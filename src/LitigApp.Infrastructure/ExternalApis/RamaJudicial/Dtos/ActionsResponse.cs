using System.Text.Json.Serialization;

namespace LitigApp.Infrastructure.ExternalApis.RamaJudicial.Dtos;

internal sealed class ActionsResponse
{
    [JsonPropertyName("actuaciones")]
    public List<ActionDto> Actions { get; init; } = [];

    [JsonPropertyName("paginacion")]
    public Pagination Pagination { get; init; } = new();
}

internal sealed class ActionDto
{
    [JsonPropertyName("idRegActuacion")]
    public long ActionId { get; init; }

    [JsonPropertyName("llaveProceso")]
    public string ProcessKey { get; init; } = "";

    [JsonPropertyName("consActuacion")]
    public int ActionNumber { get; init; }

    [JsonPropertyName("fechaActuacion")]
    public DateTime? ActionDate { get; init; }

    [JsonPropertyName("actuacion")]
    public string ActionType { get; init; } = "";

    [JsonPropertyName("anotacion")]
    public string? Note { get; init; }

    [JsonPropertyName("fechaInicial")]
    public DateTime? StartDate { get; init; }

    [JsonPropertyName("fechaFinal")]
    public DateTime? EndDate { get; init; }

    [JsonPropertyName("fechaRegistro")]
    public DateTime? RegistrationDate { get; init; }

    // Spike finding §4: comes with trailing spaces — caller must .Trim()
    [JsonPropertyName("codRegla")]
    public string? RuleCode { get; init; }

    [JsonPropertyName("conDocumentos")]
    public bool HasDocuments { get; init; }

    // Spike finding §4: extra field — total actions count, ignored
    [JsonPropertyName("cant")]
    public int TotalCount { get; init; }
}
