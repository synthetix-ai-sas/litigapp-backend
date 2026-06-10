using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Catalog.Dtos;

namespace LitigApp.Application.Features.Catalog.Queries.ListCourtsByCity;

public sealed class ListCourtsByCityHandler : IQueryHandler<ListCourtsByCityQuery, List<CourtDto>>
{
    private readonly ICatalogReader _reader;

    public ListCourtsByCityHandler(ICatalogReader reader) => _reader = reader;

    public Task<List<CourtDto>> HandleAsync(ListCourtsByCityQuery query, CancellationToken ct = default)
        => _reader.ListCourtsByCityAsync(query.CityId, query.SpecialtyCode, query.EntityCode, ct);
}
