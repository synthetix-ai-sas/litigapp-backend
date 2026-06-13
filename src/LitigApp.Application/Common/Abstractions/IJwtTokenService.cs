namespace LitigApp.Application.Common.Abstractions;

public interface IJwtTokenService
{
    string GenerateAccessToken(string userId, string email, IEnumerable<string> roles);
    string GenerateRefreshToken();
    string HashRefreshToken(string token);
    bool ValidateRefreshToken(string token, string storedHash);
    string? GetUserIdFromAccessToken(string accessToken);
}
