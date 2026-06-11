using System.Text.Json.Serialization;

namespace LitigApp.Infrastructure.ExternalApis.RamaJudicial.Dtos;

internal sealed class SubjectsResponse
{
    [JsonPropertyName("sujetos")]
    public List<SubjectDto> Subjects { get; init; } = [];

    [JsonPropertyName("paginacion")]
    public Pagination Pagination { get; init; } = new();
}

internal sealed class SubjectDto
{
    [JsonPropertyName("idRegSujeto")]
    public long SubjectId { get; init; }

    [JsonPropertyName("tipoSujeto")]
    public string SubjectType { get; init; } = "";

    [JsonPropertyName("esEmplazado")]
    public bool IsServedByPublication { get; init; }

    [JsonPropertyName("identificacion")]
    public string? Identification { get; init; }

    [JsonPropertyName("nombreRazonSocial")]
    public string FullName { get; init; } = "";

    // Spike finding §4: extra field — total subjects count, ignored
    [JsonPropertyName("cant")]
    public int TotalCount { get; init; }
}
