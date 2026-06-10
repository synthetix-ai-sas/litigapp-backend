using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Catalog.Dtos;

namespace LitigApp.Application.Features.Catalog.Queries.ListCitiesByDepartment;

public sealed record ListCitiesByDepartmentQuery(string DepartmentId) : IQuery<List<CityDto>>;
