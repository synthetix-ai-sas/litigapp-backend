using LitigApp.Application.Common.Abstractions;
using LitigApp.Domain.Auth;
using LitigApp.Domain.Common;

namespace LitigApp.Application.Features.Auth.Commands.Login;

public sealed class LoginCommandHandler : ICommandHandler<LoginCommand, AuthTokensResponse>
{
    private const string InvalidCredentialsError = "Invalid credentials.";

    private readonly IIdentityService _identityService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IAuthRepository _authRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public LoginCommandHandler(
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

    public async Task<Result<AuthTokensResponse>> HandleAsync(LoginCommand command, CancellationToken ct = default)
    {
        var userId = await _identityService.ValidateCredentialsAsync(command.Email, command.Password, ct);
        if (userId is null)
            return Result<AuthTokensResponse>.Failure(InvalidCredentialsError);

        var roles = await _identityService.GetUserRolesAsync(userId, ct);
        var accessToken = _jwtTokenService.GenerateAccessToken(userId, command.Email, roles);
        var rawRefresh = _jwtTokenService.GenerateRefreshToken();
        var tokenHash = _jwtTokenService.HashRefreshToken(rawRefresh);
        var now = _dateTimeProvider.UtcNow;

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = new DateTimeOffset(now, TimeSpan.Zero).AddDays(_jwtTokenService.RefreshTokenExpiresInDays),
            CreatedAt = new DateTimeOffset(now, TimeSpan.Zero)
        };

        await _authRepository.AddRefreshTokenAsync(refreshToken, ct);
        await _authRepository.SaveChangesAsync(ct);

        return Result<AuthTokensResponse>.Success(
            new AuthTokensResponse(accessToken, rawRefresh, _jwtTokenService.AccessTokenExpiresInSeconds));
    }
}
