namespace LitigApp.Infrastructure.ExternalApis.RamaJudicial.Dtos;

internal sealed class OverviewResponse
{
    public string TipoConsulta { get; init; } = "";
    public List<OverviewProcess> Procesos { get; init; } = [];
    public Pagination Paginacion { get; init; } = new();
}

internal sealed class OverviewProcess
{
    public long IdProceso { get; init; }
    public int IdConexion { get; init; }
    public string LlaveProceso { get; init; } = "";
    public DateTime? FechaUltimaActuacion { get; init; }
    public string Despacho { get; init; } = "";
    public string Departamento { get; init; } = "";
    public bool EsPrivado { get; init; }
    // Spike finding §4: extra field present in real API, not in blueprint DTO
    public int CantFilas { get; init; }
}
