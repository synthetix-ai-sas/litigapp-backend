namespace LitigApp.Infrastructure.ExternalApis.RamaJudicial.Dtos;

internal sealed class Pagination
{
    public int CantidadRegistros { get; init; }
    public int RegistrosPagina { get; init; }
    public int CantidadPaginas { get; init; }
    public int Pagina { get; init; }
}
