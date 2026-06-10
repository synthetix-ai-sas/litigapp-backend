using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Catalog.Dtos;

namespace LitigApp.Application.Features.Catalog.Queries.ListDepartments;

public sealed class ListDepartmentsHandler : IQueryHandler<ListDepartmentsQuery, List<DepartmentDto>>
{
    private readonly ICatalogReader _reader;

    public ListDepartmentsHandler(ICatalogReader reader) => _reader = reader;

    public Task<List<DepartmentDto>> HandleAsync(ListDepartmentsQuery query, CancellationToken ct = default)
        => _reader.ListDepartmentsAsync(ct);
}
