using LitigApp.Application.Catalog.Queries.ListCitiesByDepartment;
using LitigApp.Application.Catalog.Queries.ListDepartments;
using LitigApp.Application.Common;
using LitigApp.Infrastructure.Catalog;
using LitigApp.Infrastructure.Identity;
using LitigApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LitigApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

        services.AddDbContext<AppDbContext>(options =>
            options
                .UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention());

        // ASP.NET Core Identity
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = false; // true in production
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        // Catalog query handlers
        services.AddScoped<IQueryHandler<ListDepartmentsQuery, List<DepartmentDto>>, ListDepartmentsHandler>();
        services.AddScoped<IQueryHandler<ListCitiesByDepartmentQuery, List<CityDto>>, ListCitiesByDepartmentHandler>();

        return services;
    }
}
