using LitigApp.Application.Common.Abstractions;

namespace LitigApp.Application.Features.Auth.Commands.RefreshToken;

public record RefreshTokenCommand(string AccessToken, string RefreshToken) : ICommand<AuthTokensResponse>;
