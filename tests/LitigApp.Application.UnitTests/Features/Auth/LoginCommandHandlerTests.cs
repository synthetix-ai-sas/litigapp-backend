using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Auth.Commands.Login;
using NSubstitute;

namespace LitigApp.Application.UnitTests.Features.Auth;

public class LoginCommandHandlerTests
{
    private readonly IIdentityService _identityService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IAuthRepository _authRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly LoginCommandHandler _sut;

    public LoginCommandHandlerTests()
    {
        _identityService = Substitute.For<IIdentityService>();
        _jwtTokenService = Substitute.For<IJwtTokenService>();
        _authRepository = Substitute.For<IAuthRepository>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();

        _sut = new LoginCommandHandler(_identityService, _jwtTokenService, _authRepository, _dateTimeProvider);
    }

    [Fact]
    public async Task HandleAsync_WhenCredentialsInvalid_ReturnsFailure()
    {
        _identityService.ValidateCredentialsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((string?)null);

        var result = await _sut.HandleAsync(new LoginCommand("user@example.com", "wrong"));

        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid credentials.", result.Error);
    }

    [Fact]
    public async Task HandleAsync_WhenCredentialsInvalid_DoesNotLeakReason()
    {
        // Both "user not found" and "wrong password" should return the same error
        _identityService.ValidateCredentialsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((string?)null);

        var result = await _sut.HandleAsync(new LoginCommand("ghost@example.com", "any"));

        Assert.Equal("Invalid credentials.", result.Error);
    }

    [Fact]
    public async Task HandleAsync_WhenSuccessful_ReturnsAccessAndRefreshTokens()
    {
        const string userId = "user-1";
        const string accessToken = "access.token.value";
        const string rawRefresh = "raw-refresh-token";
        const string hashedRefresh = "hashed-refresh";

        _identityService.ValidateCredentialsAsync("user@example.com", "password123", Arg.Any<CancellationToken>())
            .Returns(userId);
        _identityService.GetUserRolesAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<string> { "User" });
        _jwtTokenService.AccessTokenExpiresInSeconds.Returns(900);
        _jwtTokenService.RefreshTokenExpiresInDays.Returns(7);
        _jwtTokenService.GenerateAccessToken(userId, "user@example.com", Arg.Any<IEnumerable<string>>())
            .Returns(accessToken);
        _jwtTokenService.GenerateRefreshToken().Returns(rawRefresh);
        _jwtTokenService.HashRefreshToken(rawRefresh).Returns(hashedRefresh);
        _dateTimeProvider.UtcNow.Returns(new DateTime(2026, 6, 12, 0, 0, 0, DateTimeKind.Utc));

        var result = await _sut.HandleAsync(new LoginCommand("user@example.com", "password123"));

        Assert.True(result.IsSuccess);
        Assert.Equal(accessToken, result.Value!.AccessToken);
        Assert.Equal(rawRefresh, result.Value.RefreshToken);
        Assert.Equal(900, result.Value.ExpiresInSeconds);
    }

    [Fact]
    public async Task HandleAsync_WhenSuccessful_PersistsRefreshToken()
    {
        const string userId = "user-1";
        _identityService.ValidateCredentialsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(userId);
        _identityService.GetUserRolesAsync(userId, Arg.Any<CancellationToken>()).Returns(new List<string>());
        _jwtTokenService.AccessTokenExpiresInSeconds.Returns(900);
        _jwtTokenService.RefreshTokenExpiresInDays.Returns(7);
        _jwtTokenService.GenerateAccessToken(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IEnumerable<string>>())
            .Returns("token");
        _jwtTokenService.GenerateRefreshToken().Returns("raw");
        _jwtTokenService.HashRefreshToken(Arg.Any<string>()).Returns("hash");
        _dateTimeProvider.UtcNow.Returns(new DateTime(2026, 6, 12, 0, 0, 0, DateTimeKind.Utc));

        await _sut.HandleAsync(new LoginCommand("user@example.com", "password123"));

        await _authRepository.Received(1).AddRefreshTokenAsync(
            Arg.Is<Domain.Auth.RefreshToken>(t =>
                t.UserId == userId &&
                t.TokenHash == "hash" &&
                t.RevokedAt == null),
            Arg.Any<CancellationToken>());
        await _authRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
