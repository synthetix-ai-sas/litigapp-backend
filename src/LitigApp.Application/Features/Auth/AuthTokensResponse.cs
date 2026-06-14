namespace LitigApp.Application.Features.Auth;

public record AuthTokensResponse(string AccessToken, string RefreshToken, int ExpiresInSeconds);
