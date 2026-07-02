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
using LitigApp.Application.Common.Models;
using LitigApp.Application.Features.Processes.Dtos;
using LitigApp.Application.Features.Processes.Queries.GetProcessById;
using LitigApp.Application.Features.Processes.Queries.ListNovelties;
using LitigApp.Application.Features.Processes.Queries.ListProcesses;
using LitigApp.Application.Features.Processes.Commands.CreateFromFileNumber;
using LitigApp.Application.Features.Processes.Commands.CreateFromWizard;
using LitigApp.Application.Features.Processes.Commands.MarkAttended;
using LitigApp.Application.Features.Processes.Commands.SoftDelete;
using LitigApp.Application.Features.Processes.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LitigApp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, bool isWorker)
    {
        // Only the api role's HTTP endpoints call these; worker jobs hit Infrastructure directly.
        if (!isWorker)
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

            // Process query handlers
            services.AddScoped<IQueryHandler<ListNoveltiesQuery, PagedResult<ProcessListItemDto>>, ListNoveltiesHandler>();
            services.AddScoped<IQueryHandler<ListProcessesQuery, PagedResult<ProcessListItemDto>>, ListProcessesHandler>();
            services.AddScoped<IQueryHandler<GetProcessByIdQuery, ProcessDetailDto?>, GetProcessByIdHandler>();

            // Process command handlers (+ shared creation service)
            services.AddScoped<ProcessCreationService>();
            services.AddScoped<IProcessCreator>(sp => sp.GetRequiredService<ProcessCreationService>());
            services.AddScoped<ICommandHandler<CreateProcessFromFileNumberCommand, ProcessDetailDto>, CreateProcessFromFileNumberHandler>();
            services.AddScoped<ICommandHandler<CreateProcessFromWizardCommand, ProcessDetailDto>, CreateProcessFromWizardHandler>();
            services.AddScoped<ICommandHandler<MarkAttendedCommand, Unit>, MarkAttendedHandler>();
            services.AddScoped<ICommandHandler<SoftDeleteProcessCommand, Unit>, SoftDeleteProcessHandler>();

            // Decorate all ICommandHandler<,> registrations with LoggingBehavior (Commands only — not Queries)
            services.Decorate(typeof(ICommandHandler<,>), typeof(LoggingBehavior<,>));
        }

        return services;
    }
}
