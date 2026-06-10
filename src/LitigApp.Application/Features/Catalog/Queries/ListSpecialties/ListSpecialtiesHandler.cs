using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Catalog.Dtos;

namespace LitigApp.Application.Features.Catalog.Queries.ListSpecialties;

public sealed class ListSpecialtiesHandler : IQueryHandler<ListSpecialtiesQuery, List<SpecialtyDto>>
{
    private readonly ICatalogReader _reader;

    public ListSpecialtiesHandler(ICatalogReader reader) => _reader = reader;

    public Task<List<SpecialtyDto>> HandleAsync(ListSpecialtiesQuery query, CancellationToken ct = default)
        => _reader.ListSpecialtiesAsync(ct);
}
