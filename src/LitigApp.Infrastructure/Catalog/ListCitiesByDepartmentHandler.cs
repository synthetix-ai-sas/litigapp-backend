using LitigApp.Application.Catalog.Queries.ListCitiesByDepartment;
using LitigApp.Application.Common;
using LitigApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LitigApp.Infrastructure.Catalog;

internal sealed class ListCitiesByDepartmentHandler(AppDbContext db)
    : IQueryHandler<ListCitiesByDepartmentQuery, List<CityDto>>
{
    public async Task<Result<List<CityDto>>> Handle(ListCitiesByDepartmentQuery query, CancellationToken ct)
    {
        var cities = await db.Cities
            .AsNoTracking()
            .Where(c => c.DepartmentId == query.DepartmentId)
            .OrderBy(c => c.Name)
            .Select(c => new CityDto(c.Id, c.Name, c.DepartmentId))
            .ToListAsync(ct);

        return Result<List<CityDto>>.Ok(cities);
    }
}
