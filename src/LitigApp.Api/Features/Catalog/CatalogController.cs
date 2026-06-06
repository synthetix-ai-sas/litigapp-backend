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
using Microsoft.AspNetCore.Mvc;

namespace LitigApp.Api.Features.Catalog;

[ApiController]
[Route("api/v1/catalog")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public sealed class CatalogController : ControllerBase
{
    [HttpGet("departments")]
    public async Task<IActionResult> GetDepartments(
        [FromServices] IQueryHandler<ListDepartmentsQuery, List<DepartmentDto>> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new ListDepartmentsQuery(), ct);
        return Ok(new { data = result, error = (object?)null });
    }

    [HttpGet("departments/{id}/cities")]
    public async Task<IActionResult> GetCitiesByDepartment(
        string id,
        [FromServices] IQueryHandler<ListCitiesByDepartmentQuery, List<CityDto>> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new ListCitiesByDepartmentQuery(id), ct);
        return Ok(new { data = result, error = (object?)null });
    }

    [HttpGet("specialties")]
    public async Task<IActionResult> GetSpecialties(
        [FromServices] IQueryHandler<ListSpecialtiesQuery, List<SpecialtyDto>> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new ListSpecialtiesQuery(), ct);
        return Ok(new { data = result, error = (object?)null });
    }

    [HttpGet("entities")]
    public async Task<IActionResult> GetEntities(
        [FromServices] IQueryHandler<ListEntitiesQuery, List<EntityDto>> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new ListEntitiesQuery(), ct);
        return Ok(new { data = result, error = (object?)null });
    }

    [HttpGet("cities/{cityId}/courts")]
    public async Task<IActionResult> GetCourtsByCity(
        string cityId,
        [FromQuery] string? specialtyCode,
        [FromQuery] string? entityCode,
        [FromServices] IQueryHandler<ListCourtsByCityQuery, List<CourtDto>> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new ListCourtsByCityQuery(cityId, specialtyCode, entityCode), ct);
        return Ok(new { data = result, error = (object?)null });
    }

    [HttpGet("courts/search")]
    public async Task<IActionResult> SearchCourts(
        [FromQuery] string name,
        [FromQuery] string? cityId,
        [FromServices] IQueryHandler<SearchCourtsQuery, List<CourtDto>> handler,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { data = (object?)null, error = new { code = "VALIDATION", message = "El parámetro 'name' es requerido." } });

        var result = await handler.HandleAsync(new SearchCourtsQuery(name, cityId), ct);
        return Ok(new { data = result, error = (object?)null });
    }
}
