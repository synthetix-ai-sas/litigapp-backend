using Microsoft.Extensions.DependencyInjection;

namespace LitigApp.Api.Auth;

public static class AuthorizationPolicies
{
    public const string User = "User";
    public const string Admin = "Admin";

    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(User, policy => policy.RequireAuthenticatedUser())
            .AddPolicy(Admin, policy => policy.RequireRole("Admin"));

        return services;
    }
}
