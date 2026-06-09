namespace LitigApp.Application.Catalog.Queries.ListCitiesByDepartment;

public sealed record ListCitiesByDepartmentQuery(string DepartmentId);

public sealed record CityDto(string Id, string Name, string DepartmentId);
