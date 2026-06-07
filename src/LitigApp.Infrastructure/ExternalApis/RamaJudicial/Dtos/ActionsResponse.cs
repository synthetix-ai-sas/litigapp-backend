namespace LitigApp.Infrastructure.ExternalApis.RamaJudicial.Dtos;

internal sealed class ActionsResponse
{
    public List<ActionDto> Actuaciones { get; init; } = [];
    public Pagination Paginacion { get; init; } = new();
}

internal sealed class ActionDto
{
    public long IdRegActuacion { get; init; }
    public string LlaveProceso { get; init; } = "";
    public int ConsActuacion { get; init; }
    public DateTime? FechaActuacion { get; init; }
    public string Actuacion { get; init; } = "";
    public string? Anotacion { get; init; }
    public DateTime? FechaInicial { get; init; }
    public DateTime? FechaFinal { get; init; }
    public DateTime? FechaRegistro { get; init; }
    // Spike finding §4: codRegla comes with trailing spaces — caller must .Trim()
    public string? CodRegla { get; init; }
    public bool ConDocumentos { get; init; }
    // Spike finding §4: extra field present in real API — total actions count, ignored
    public int Cant { get; init; }
}
