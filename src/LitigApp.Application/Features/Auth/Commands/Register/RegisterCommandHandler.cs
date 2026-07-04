using System.Net;
using LitigApp.Application.Common.Abstractions;
using LitigApp.Domain.Auth;
using LitigApp.Domain.Common;
using RefreshTokenEntity = LitigApp.Domain.Auth.RefreshToken;

namespace LitigApp.Application.Features.Auth.Commands.Register;

public sealed class RegisterCommandHandler : ICommandHandler<RegisterCommand, AuthTokensResponse>
{
    private readonly IIdentityService _identityService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IAuthRepository _authRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RegisterCommandHandler(
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

    public async Task<Result<AuthTokensResponse>> HandleAsync(RegisterCommand command, CancellationToken ct = default)
    {
        if (!command.AcceptedTerms || !command.AcceptedPrivacy)
            return Result<AuthTokensResponse>.Failure("LEGAL_NOT_ACCEPTED");

        if (await _identityService.IsEmailRegisteredAsync(command.Email, ct))
            return Result<AuthTokensResponse>.Failure("Email is already registered.");

        var createResult = await _identityService.CreateUserAsync(
            command.Email, command.Password, command.FullName, command.WhatsAppPhone, ct);

        if (!createResult.Succeeded)
            return Result<AuthTokensResponse>.Failure(createResult.Error!);

        var userId = createResult.UserId!;
        var roles = await _identityService.GetUserRolesAsync(userId, ct);
        var accessToken = _jwtTokenService.GenerateAccessToken(userId, command.Email, roles);
        var rawRefresh = _jwtTokenService.GenerateRefreshToken();
        var tokenHash = _jwtTokenService.HashRefreshToken(rawRefresh);
        var now = _dateTimeProvider.UtcNow;
        var acceptedAt = new DateTimeOffset(now, TimeSpan.Zero);

        var refreshToken = new RefreshTokenEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = acceptedAt.AddDays(_jwtTokenService.RefreshTokenExpiresInDays),
            CreatedAt = acceptedAt
        };

        await _authRepository.AddRefreshTokenAsync(refreshToken, ct);

        IPAddress.TryParse(command.IpAddress, out var clientIp);

        await _authRepository.AddLegalAcceptanceAsync(new LegalAcceptance
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DocumentType = "terms",
            DocumentVersion = command.TermsVersion,
            AcceptedAt = acceptedAt,
            IpAddress = clientIp
        }, ct);

        await _authRepository.AddLegalAcceptanceAsync(new LegalAcceptance
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DocumentType = "privacy",
            DocumentVersion = command.PrivacyVersion,
            AcceptedAt = acceptedAt,
            IpAddress = clientIp
        }, ct);

        await _authRepository.SaveChangesAsync(ct);

        return Result<AuthTokensResponse>.Success(
            new AuthTokensResponse(accessToken, rawRefresh, _jwtTokenService.AccessTokenExpiresInSeconds));
    }
}
