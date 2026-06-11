using System.Text.Json.Serialization;

namespace LitigApp.Infrastructure.ExternalApis.RamaJudicial.Dtos;

internal sealed class Pagination
{
    [JsonPropertyName("cantidadRegistros")]
    public int TotalRecords { get; init; }

    [JsonPropertyName("registrosPagina")]
    public int PageSize { get; init; }

    [JsonPropertyName("cantidadPaginas")]
    public int TotalPages { get; init; }

    [JsonPropertyName("pagina")]
    public int CurrentPage { get; init; }
}
