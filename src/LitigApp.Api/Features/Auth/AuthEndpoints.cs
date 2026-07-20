using System.Security.Claims;
using LitigApp.Api.Auth;
using LitigApp.Api.Filters;
using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Auth;
using LitigApp.Application.Features.Auth.Commands.Login;
using LitigApp.Application.Features.Auth.Commands.Register;
using LitigApp.Application.Features.Auth.Commands.RefreshToken;
using LitigApp.Application.Features.Auth.Commands.RequestPasswordReset;
using LitigApp.Application.Features.Auth.Commands.ResetPassword;
using LitigApp.Domain.Common;
using LitigApp.Infrastructure.Identity;
using LitigApp.Infrastructure.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace LitigApp.Api.Features.Auth;

public record RegisterRequest(
    string Email,
    string Password,
    string FullName,
    string? WhatsAppPhone,
    bool AcceptedTerms,
    bool AcceptedPrivacy);

public record PasswordResetRequestedResponse(string Message);

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
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
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

        group.MapPost("/refresh", RefreshAsync)
            .WithName("RefreshToken")
            .WithSummary("Rotate a refresh token and issue new token pair")
            .Produces<AuthTokensResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .AllowAnonymous();

        group.MapPost("/password-reset/request", RequestPasswordResetAsync)
            .WithName("RequestPasswordReset")
            .WithSummary("Request a password reset email")
            .Produces<PasswordResetRequestedResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .RequireRateLimiting("password-reset")
            .AllowAnonymous();

        group.MapPost("/password-reset/confirm", ConfirmPasswordResetAsync)
            .WithName("ConfirmPasswordReset")
            .WithSummary("Reset password using the token received by email")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .AddEndpointFilter<ValidationFilter<ResetPasswordCommand>>()
            .AllowAnonymous();

        return app;
    }

    private static async Task<IResult> RegisterAsync(
        [FromBody] RegisterRequest request,
        HttpContext httpContext,
        IOptions<LegalOptions> legalOptions,
        ICommandHandler<RegisterCommand, AuthTokensResponse> handler,
        CancellationToken ct)
    {
        var legal = legalOptions.Value;
        var command = new RegisterCommand(
            request.Email,
            request.Password,
            request.FullName,
            request.WhatsAppPhone,
            request.AcceptedTerms,
            request.AcceptedPrivacy,
            GetClientIp(httpContext),
            legal.TermsVersion,
            legal.PrivacyVersion);

        var result = await handler.HandleAsync(command, ct);

        if (result.IsSuccess)
            return TypedResults.Created((string?)null, result.Value);

        if (result.Error == "LEGAL_NOT_ACCEPTED")
            return TypedResults.Problem(
                detail: "You must accept the Terms of Service and Privacy Policy to create an account.",
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Legal consent required.",
                extensions: new Dictionary<string, object?> { ["code"] = "LEGAL_NOT_ACCEPTED" });

        return TypedResults.Problem(
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

    private static async Task<IResult> RefreshAsync(
        [FromBody] RefreshTokenCommand command,
        ICommandHandler<RefreshTokenCommand, AuthTokensResponse> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(command, ct);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : TypedResults.Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Token refresh failed.");
    }

    private static async Task<IResult> RequestPasswordResetAsync(
        [FromBody] RequestPasswordResetCommand command,
        ICommandHandler<RequestPasswordResetCommand, Unit> handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(command, ct);
        // Always 200 with a neutral message — never reveal whether the email exists.
        return TypedResults.Ok(new PasswordResetRequestedResponse(
            "Si el correo está registrado, recibirás las instrucciones para restablecer tu contraseña."));
    }

    private static async Task<IResult> ConfirmPasswordResetAsync(
        [FromBody] ResetPasswordCommand command,
        ICommandHandler<ResetPasswordCommand, Unit> handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(command, ct);

        return result.IsSuccess
            ? TypedResults.NoContent()
            : TypedResults.Problem(
                detail: "El enlace de restablecimiento es inválido o ha expirado.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Restablecimiento de contraseña fallido.",
                extensions: new Dictionary<string, object?> { ["code"] = "INVALID_TOKEN" });
    }

    private static string? GetClientIp(HttpContext ctx)
    {
        var forwarded = ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
            return forwarded.Split(',')[0].Trim();
        return ctx.Connection.RemoteIpAddress?.ToString();
    }
}

public record MeResponse(string UserId, string Email);
