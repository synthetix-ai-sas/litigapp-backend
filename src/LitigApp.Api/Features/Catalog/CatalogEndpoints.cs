using LitigApp.Application.Catalog.Queries.ListCitiesByDepartment;
using LitigApp.Application.Catalog.Queries.ListDepartments;
using LitigApp.Application.Common;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LitigApp.Api.Features.Catalog;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/catalog")
            .RequireAuthorization()
            .WithTags("Catalog");

        group.MapGet("/departments", ListDepartments)
            .WithName("ListDepartments")
            .WithSummary("Lista todos los departamentos");

        group.MapGet("/departments/{id}/cities", ListCitiesByDepartment)
            .WithName("ListCitiesByDepartment")
            .WithSummary("Lista municipios de un departamento");

        return app;
    }

    private static async Task<Ok<List<DepartmentDto>>> ListDepartments(
        IQueryHandler<ListDepartmentsQuery, List<DepartmentDto>> handler,
        CancellationToken ct)
    {
        var result = await handler.Handle(new ListDepartmentsQuery(), ct);
        return TypedResults.Ok(result.Value!);
    }

    private static async Task<Ok<List<CityDto>>> ListCitiesByDepartment(
        string id,
        IQueryHandler<ListCitiesByDepartmentQuery, List<CityDto>> handler,
        CancellationToken ct)
    {
        var result = await handler.Handle(new ListCitiesByDepartmentQuery(id), ct);
        return TypedResults.Ok(result.Value!);
    }
}
