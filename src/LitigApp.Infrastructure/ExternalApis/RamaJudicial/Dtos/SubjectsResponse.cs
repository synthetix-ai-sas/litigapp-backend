namespace LitigApp.Infrastructure.ExternalApis.RamaJudicial.Dtos;

internal sealed class SubjectsResponse
{
    public List<SubjectDto> Sujetos { get; init; } = [];
    public Pagination Paginacion { get; init; } = new();
}

internal sealed class SubjectDto
{
    public long IdRegSujeto { get; init; }
    public string TipoSujeto { get; init; } = "";
    public bool EsEmplazado { get; init; }
    public string? Identificacion { get; init; }
    public string NombreRazonSocial { get; init; } = "";
    // Spike finding §4: extra field present in real API — total subjects count, ignored
    public int Cant { get; init; }
}
