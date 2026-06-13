using LitigApp.Application.Common.Abstractions;
using LitigApp.Domain.Common;
using RefreshTokenEntity = LitigApp.Domain.Auth.RefreshToken;

namespace LitigApp.Application.Features.Auth.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, AuthTokensResponse>
{
    private readonly IIdentityService _identityService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IAuthRepository _authRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RefreshTokenCommandHandler(
        IIdentityService identityService,
        IJwtTokenService jwtTokenService,
        IAuthRepository authRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _identityService = identityService;
        _jwtTokenService = jwtTokenService;
        _authRepository = authRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<AuthTokensResponse>> HandleAsync(RefreshTokenCommand command, CancellationToken ct = default)
    {
        var userId = _jwtTokenService.GetUserIdFromAccessToken(command.AccessToken);
        if (userId is null)
            return Result<AuthTokensResponse>.Failure("Invalid access token.");

        var tokenHash = _jwtTokenService.HashRefreshToken(command.RefreshToken);
        var storedToken = await _authRepository.FindActiveRefreshTokenAsync(userId, tokenHash, ct);
        if (storedToken is null)
            return Result<AuthTokensResponse>.Failure("Invalid or expired refresh token.");

        // Revoke old token (rotation)
        storedToken.RevokedAt = DateTimeOffset.UtcNow;

        var email = await _identityService.GetUserEmailAsync(userId, ct) ?? string.Empty;
        var roles = await _identityService.GetUserRolesAsync(userId, ct);
        var newAccessToken = _jwtTokenService.GenerateAccessToken(userId, email, roles);
        var newRawRefresh = _jwtTokenService.GenerateRefreshToken();
        var newHash = _jwtTokenService.HashRefreshToken(newRawRefresh);
        var now = _dateTimeProvider.UtcNow;

        var newToken = new RefreshTokenEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = newHash,
            ExpiresAt = new DateTimeOffset(now, TimeSpan.Zero).AddDays(_jwtTokenService.RefreshTokenExpiresInDays),
            CreatedAt = new DateTimeOffset(now, TimeSpan.Zero)
        };

        await _authRepository.AddRefreshTokenAsync(newToken, ct);
        await _authRepository.SaveChangesAsync(ct);

        return Result<AuthTokensResponse>.Success(
            new AuthTokensResponse(newAccessToken, newRawRefresh, _jwtTokenService.AccessTokenExpiresInSeconds));
    }
}
