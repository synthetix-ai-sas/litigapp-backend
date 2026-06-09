using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Catalog.Dtos;
using LitigApp.Application.Features.Catalog.Queries.ListCitiesByDepartment;
using LitigApp.Application.Features.Catalog.Queries.ListCourtsByCity;
using LitigApp.Application.Features.Catalog.Queries.ListDepartments;
using LitigApp.Application.Features.Catalog.Queries.ListEntities;
using LitigApp.Application.Features.Catalog.Queries.ListSpecialties;
using LitigApp.Application.Features.Catalog.Queries.SearchCourts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace LitigApp.Api.Features.Catalog;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/catalog")
            .RequireAuthorization(new AuthorizeAttribute
            {
                AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme
            })
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

    private static async Task<IResult> ListDepartments(
        IQueryHandler<ListDepartmentsQuery, List<DepartmentDto>> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new ListDepartmentsQuery(), ct);
        return Results.Ok(new { data = result, error = (object?)null });
    }

    private static async Task<IResult> ListCitiesByDepartment(
        string id,
        IQueryHandler<ListCitiesByDepartmentQuery, List<CityDto>> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new ListCitiesByDepartmentQuery(id), ct);
        return Results.Ok(new { data = result, error = (object?)null });
    }

    private static async Task<IResult> ListSpecialties(
        IQueryHandler<ListSpecialtiesQuery, List<SpecialtyDto>> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new ListSpecialtiesQuery(), ct);
        return Results.Ok(new { data = result, error = (object?)null });
    }

    private static async Task<IResult> ListEntities(
        IQueryHandler<ListEntitiesQuery, List<EntityDto>> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new ListEntitiesQuery(), ct);
        return Results.Ok(new { data = result, error = (object?)null });
    }

    private static async Task<IResult> ListCourtsByCity(
        string cityId,
        IQueryHandler<ListCourtsByCityQuery, List<CourtDto>> handler,
        CancellationToken ct,
        string? specialtyCode = null,
        string? entityCode = null)
    {
        var result = await handler.HandleAsync(new ListCourtsByCityQuery(cityId, specialtyCode, entityCode), ct);
        return Results.Ok(new { data = result, error = (object?)null });
    }

    private static async Task<IResult> SearchCourts(
        IQueryHandler<SearchCourtsQuery, List<CourtDto>> handler,
        CancellationToken ct,
        string? name = null,
        string? cityId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Results.BadRequest(new { data = (object?)null, error = new { code = "VALIDATION", message = "El parámetro 'name' es requerido." } });

        var result = await handler.HandleAsync(new SearchCourtsQuery(name, cityId), ct);
        return Results.Ok(new { data = result, error = (object?)null });
    }
}
