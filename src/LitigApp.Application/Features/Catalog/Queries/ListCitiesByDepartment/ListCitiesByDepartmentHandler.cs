using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Catalog.Dtos;

namespace LitigApp.Application.Features.Catalog.Queries.ListCitiesByDepartment;

public sealed class ListCitiesByDepartmentHandler : IQueryHandler<ListCitiesByDepartmentQuery, List<CityDto>>
{
    private readonly ICatalogReader _reader;

    public ListCitiesByDepartmentHandler(ICatalogReader reader) => _reader = reader;

    public Task<List<CityDto>> HandleAsync(ListCitiesByDepartmentQuery query, CancellationToken ct = default)
        => _reader.ListCitiesByDepartmentAsync(query.DepartmentId, ct);
}
