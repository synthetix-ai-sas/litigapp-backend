using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Auth.Commands.Register;
using NSubstitute;

namespace LitigApp.Application.UnitTests.Features.Auth;

public class RegisterCommandHandlerTests
{
    private readonly IIdentityService _identityService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IAuthRepository _authRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly RegisterCommandHandler _sut;

    public RegisterCommandHandlerTests()
    {
        _identityService = Substitute.For<IIdentityService>();
        _jwtTokenService = Substitute.For<IJwtTokenService>();
        _authRepository = Substitute.For<IAuthRepository>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();

        _sut = new RegisterCommandHandler(_identityService, _jwtTokenService, _authRepository, _dateTimeProvider);
    }

    [Fact]
    public async Task HandleAsync_WhenEmailAlreadyTaken_ReturnsFailure()
    {
        _identityService.IsEmailRegisteredAsync("taken@example.com", Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _sut.HandleAsync(new RegisterCommand("taken@example.com", "password123", "Juan", null));

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task HandleAsync_WhenIdentityCreateFails_ReturnsFailure()
    {
        _identityService.IsEmailRegisteredAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _identityService.CreateUserAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(new IdentityOperationResult(false, null, "Password too weak."));

        var result = await _sut.HandleAsync(new RegisterCommand("new@example.com", "weak", "Juan", null));

        Assert.False(result.IsSuccess);
        Assert.Equal("Password too weak.", result.Error);
    }

    [Fact]
    public async Task HandleAsync_WhenSuccessful_ReturnsAccessAndRefreshTokens()
    {
        const string userId = "user-abc";
        const string accessToken = "access.token.value";
        const string rawRefresh = "raw-refresh-token";
        const string hashedRefresh = "hashed-refresh-token";

        _identityService.IsEmailRegisteredAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _identityService.CreateUserAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new IdentityOperationResult(true, userId, null));
        _identityService.GetUserRolesAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<string> { "User" });
        _jwtTokenService.AccessTokenExpiresInSeconds.Returns(900);
        _jwtTokenService.RefreshTokenExpiresInDays.Returns(7);
        _jwtTokenService.GenerateAccessToken(userId, "new@example.com", Arg.Any<IEnumerable<string>>())
            .Returns(accessToken);
        _jwtTokenService.GenerateRefreshToken().Returns(rawRefresh);
        _jwtTokenService.HashRefreshToken(rawRefresh).Returns(hashedRefresh);
        _dateTimeProvider.UtcNow.Returns(new DateTime(2026, 6, 12, 0, 0, 0, DateTimeKind.Utc));

        var result = await _sut.HandleAsync(new RegisterCommand("new@example.com", "password123", "Juan Pérez", "+573001234567"));

        Assert.True(result.IsSuccess);
        Assert.Equal(accessToken, result.Value!.AccessToken);
        Assert.Equal(rawRefresh, result.Value.RefreshToken);
        Assert.Equal(900, result.Value.ExpiresInSeconds);
        await _authRepository.Received(1).AddRefreshTokenAsync(
            Arg.Is<Domain.Auth.RefreshToken>(t =>
                t.UserId == userId &&
                t.TokenHash == hashedRefresh &&
                t.RevokedAt == null),
            Arg.Any<CancellationToken>());
        await _authRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenSuccessful_DoesNotPersistTokenIfCreateFails()
    {
        _identityService.IsEmailRegisteredAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _identityService.CreateUserAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new IdentityOperationResult(false, null, "Error."));

        await _sut.HandleAsync(new RegisterCommand("new@example.com", "password123", "Juan", null));

        await _authRepository.DidNotReceive().AddRefreshTokenAsync(
            Arg.Any<Domain.Auth.RefreshToken>(), Arg.Any<CancellationToken>());
    }
}
