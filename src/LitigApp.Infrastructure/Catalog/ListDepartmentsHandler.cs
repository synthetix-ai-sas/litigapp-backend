using LitigApp.Application.Catalog.Queries.ListDepartments;
using LitigApp.Application.Common;
using LitigApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LitigApp.Infrastructure.Catalog;

internal sealed class ListDepartmentsHandler(AppDbContext db)
    : IQueryHandler<ListDepartmentsQuery, List<DepartmentDto>>
{
    public async Task<Result<List<DepartmentDto>>> Handle(ListDepartmentsQuery query, CancellationToken ct)
    {
        var departments = await db.Departments
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentDto(d.Id, d.Name))
            .ToListAsync(ct);

        return Result<List<DepartmentDto>>.Ok(departments);
    }
}
