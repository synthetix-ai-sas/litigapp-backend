namespace LitigApp.Infrastructure.ExternalApis.RamaJudicial.Dtos;

internal sealed class ProcessDetailResponse
{
    // Spike finding §3: idRegProceso (8-digit) is distinct from idProceso (9-digit).
    // We do NOT persist idRegProceso — external_process_id uses idProceso from overview.
    public long IdRegProceso { get; init; }
    public string LlaveProceso { get; init; } = "";
    public int IdConexion { get; init; }
    public bool EsPrivado { get; init; }
    public DateTime? FechaProceso { get; init; }
    public string CodDespachoCompleto { get; init; } = "";
    public string Despacho { get; init; } = "";
    public string? Ponente { get; init; }
    public string? TipoProceso { get; init; }
    public string? ClaseProceso { get; init; }
    public string? SubclaseProceso { get; init; }
    public string? Recurso { get; init; }
    public string? ContenidoRadicacion { get; init; }
}
