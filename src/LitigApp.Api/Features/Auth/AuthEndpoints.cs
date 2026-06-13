using System.Security.Claims;
using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Auth;
using LitigApp.Application.Features.Auth.Commands.Login;
using LitigApp.Application.Features.Auth.Commands.Register;
using LitigApp.Api.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace LitigApp.Api.Features.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth")
            .WithTags("Auth");

        group.MapPost("/register", RegisterAsync)
            .WithName("Register")
            .WithSummary("Register a new user account")
            .Produces<AuthTokensResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .AllowAnonymous();

        group.MapPost("/login", LoginAsync)
            .WithName("Login")
            .WithSummary("Authenticate with email and password")
            .Produces<AuthTokensResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .AllowAnonymous();

        group.MapGet("/me", GetMeAsync)
            .WithName("GetCurrentUser")
            .WithSummary("Get current authenticated user info")
            .Produces<MeResponse>(StatusCodes.Status200OK)
            .RequireAuthorization(AuthorizationPolicies.User);

        return app;
    }

    private static async Task<IResult> RegisterAsync(
        [FromBody] RegisterCommand command,
        ICommandHandler<RegisterCommand, AuthTokensResponse> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(command, ct);

        return result.IsSuccess
            ? TypedResults.Created((string?)null, result.Value)
            : TypedResults.Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status409Conflict,
                title: "Registration failed.");
    }

    private static async Task<IResult> LoginAsync(
        [FromBody] LoginCommand command,
        ICommandHandler<LoginCommand, AuthTokensResponse> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(command, ct);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : TypedResults.Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Authentication failed.");
    }

    private static IResult GetMeAsync(ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        var email = user.FindFirstValue(JwtRegisteredClaimNames.Email);
        return TypedResults.Ok(new MeResponse(userId!, email!));
    }
}

public record MeResponse(string UserId, string Email);
