using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Catalog.Dtos;

namespace LitigApp.Application.Features.Catalog.Queries.ListEntities;

public sealed class ListEntitiesHandler : IQueryHandler<ListEntitiesQuery, List<EntityDto>>
{
    private readonly ICatalogReader _reader;

    public ListEntitiesHandler(ICatalogReader reader) => _reader = reader;

    public Task<List<EntityDto>> HandleAsync(ListEntitiesQuery query, CancellationToken ct = default)
        => _reader.ListEntitiesAsync(ct);
}
