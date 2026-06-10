using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Catalog.Dtos;
using LitigApp.Application.Features.Catalog.Queries.ListCitiesByDepartment;
using LitigApp.Application.Features.Catalog.Queries.ListCourtsByCity;
using LitigApp.Application.Features.Catalog.Queries.ListDepartments;
using LitigApp.Application.Features.Catalog.Queries.ListEntities;
using LitigApp.Application.Features.Catalog.Queries.ListSpecialties;
using LitigApp.Application.Features.Catalog.Queries.SearchCourts;
using Microsoft.Extensions.DependencyInjection;

namespace LitigApp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<ListDepartmentsQuery, List<DepartmentDto>>, ListDepartmentsHandler>();
        services.AddScoped<IQueryHandler<ListCitiesByDepartmentQuery, List<CityDto>>, ListCitiesByDepartmentHandler>();
        services.AddScoped<IQueryHandler<ListSpecialtiesQuery, List<SpecialtyDto>>, ListSpecialtiesHandler>();
        services.AddScoped<IQueryHandler<ListEntitiesQuery, List<EntityDto>>, ListEntitiesHandler>();
        services.AddScoped<IQueryHandler<ListCourtsByCityQuery, List<CourtDto>>, ListCourtsByCityHandler>();
        services.AddScoped<IQueryHandler<SearchCourtsQuery, List<CourtDto>>, SearchCourtsHandler>();

        return services;
    }
}
