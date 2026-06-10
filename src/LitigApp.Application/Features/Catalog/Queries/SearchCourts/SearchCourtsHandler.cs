using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Catalog.Dtos;

namespace LitigApp.Application.Features.Catalog.Queries.SearchCourts;

public sealed class SearchCourtsHandler : IQueryHandler<SearchCourtsQuery, List<CourtDto>>
{
    private readonly ICatalogReader _reader;

    public SearchCourtsHandler(ICatalogReader reader) => _reader = reader;

    public Task<List<CourtDto>> HandleAsync(SearchCourtsQuery query, CancellationToken ct = default)
        => _reader.SearchCourtsAsync(query.NameLike, query.CityId, ct);
}
