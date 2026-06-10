using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Catalog.Dtos;
using LitigApp.Application.Features.Catalog.Queries.ListCitiesByDepartment;
using LitigApp.Application.Features.Catalog.Queries.ListCourtsByCity;
using LitigApp.Application.Features.Catalog.Queries.ListDepartments;
using LitigApp.Application.Features.Catalog.Queries.ListEntities;
using LitigApp.Application.Features.Catalog.Queries.ListSpecialties;
using LitigApp.Application.Features.Catalog.Queries.SearchCourts;
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

        group.MapGet("/specialties", ListSpecialties)
            .WithName("ListSpecialties")
            .WithSummary("Lista todas las especialidades");

        group.MapGet("/entities", ListEntities)
            .WithName("ListEntities")
            .WithSummary("Lista todas las entidades");

        group.MapGet("/cities/{cityId}/courts", ListCourtsByCity)
            .WithName("ListCourtsByCity")
            .WithSummary("Lista despachos de un municipio");

        group.MapGet("/courts/search", SearchCourts)
            .WithName("SearchCourts")
            .WithSummary("Busca despachos por nombre");

        return app;
    }

    private static async Task<Ok<List<DepartmentDto>>> ListDepartments(
        IQueryHandler<ListDepartmentsQuery, List<DepartmentDto>> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new ListDepartmentsQuery(), ct);
        return TypedResults.Ok(result);
    }

    private static async Task<Ok<List<CityDto>>> ListCitiesByDepartment(
        string id,
        IQueryHandler<ListCitiesByDepartmentQuery, List<CityDto>> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new ListCitiesByDepartmentQuery(id), ct);
        return TypedResults.Ok(result);
    }

    private static async Task<Ok<List<SpecialtyDto>>> ListSpecialties(
        IQueryHandler<ListSpecialtiesQuery, List<SpecialtyDto>> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new ListSpecialtiesQuery(), ct);
        return TypedResults.Ok(result);
    }

    private static async Task<Ok<List<EntityDto>>> ListEntities(
        IQueryHandler<ListEntitiesQuery, List<EntityDto>> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new ListEntitiesQuery(), ct);
        return TypedResults.Ok(result);
    }

    private static async Task<Ok<List<CourtDto>>> ListCourtsByCity(
        string cityId,
        IQueryHandler<ListCourtsByCityQuery, List<CourtDto>> handler,
        CancellationToken ct,
        string? specialtyCode = null,
        string? entityCode = null)
    {
        var result = await handler.HandleAsync(new ListCourtsByCityQuery(cityId, specialtyCode, entityCode), ct);
        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<List<CourtDto>>, BadRequest>> SearchCourts(
        IQueryHandler<SearchCourtsQuery, List<CourtDto>> handler,
        CancellationToken ct,
        string? name = null,
        string? cityId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return TypedResults.BadRequest();

        var result = await handler.HandleAsync(new SearchCourtsQuery(name, cityId), ct);
        return TypedResults.Ok(result);
    }
}
