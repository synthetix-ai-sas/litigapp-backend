using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Common.Behaviors;
using LitigApp.Application.Features.Auth;
using LitigApp.Application.Features.Auth.Commands.Login;
using LitigApp.Application.Features.Auth.Commands.RefreshToken;
using LitigApp.Application.Features.Auth.Commands.Register;
using LitigApp.Application.Features.Auth.Commands.RequestPasswordReset;
using LitigApp.Application.Features.Auth.Commands.ResetPassword;
using LitigApp.Domain.Common;
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
        // Auth command handlers
        services.AddScoped<ICommandHandler<RegisterCommand, AuthTokensResponse>, RegisterCommandHandler>();
        services.AddScoped<ICommandHandler<LoginCommand, AuthTokensResponse>, LoginCommandHandler>();
        services.AddScoped<ICommandHandler<RefreshTokenCommand, AuthTokensResponse>, RefreshTokenCommandHandler>();
        services.AddScoped<ICommandHandler<RequestPasswordResetCommand, Unit>, RequestPasswordResetCommandHandler>();
        services.AddScoped<ICommandHandler<ResetPasswordCommand, Unit>, ResetPasswordCommandHandler>();

        // Catalog query handlers
        services.AddScoped<IQueryHandler<ListDepartmentsQuery, List<DepartmentDto>>, ListDepartmentsHandler>();
        services.AddScoped<IQueryHandler<ListCitiesByDepartmentQuery, List<CityDto>>, ListCitiesByDepartmentHandler>();
        services.AddScoped<IQueryHandler<ListSpecialtiesQuery, List<SpecialtyDto>>, ListSpecialtiesHandler>();
        services.AddScoped<IQueryHandler<ListEntitiesQuery, List<EntityDto>>, ListEntitiesHandler>();
        services.AddScoped<IQueryHandler<ListCourtsByCityQuery, List<CourtDto>>, ListCourtsByCityHandler>();
        services.AddScoped<IQueryHandler<SearchCourtsQuery, List<CourtDto>>, SearchCourtsHandler>();

        // Decorate all ICommandHandler<,> registrations with LoggingBehavior (Commands only — not Queries)
        services.Decorate(typeof(ICommandHandler<,>), typeof(LoggingBehavior<,>));

        return services;
    }
}
