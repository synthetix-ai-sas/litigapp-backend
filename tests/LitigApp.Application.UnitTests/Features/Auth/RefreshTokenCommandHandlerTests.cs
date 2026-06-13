using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Auth.Commands.RefreshToken;
using NSubstitute;

namespace LitigApp.Application.UnitTests.Features.Auth;

public class RefreshTokenCommandHandlerTests
{
    private readonly IIdentityService _identityService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IAuthRepository _authRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly RefreshTokenCommandHandler _sut;

    public RefreshTokenCommandHandlerTests()
    {
        _identityService = Substitute.For<IIdentityService>();
        _jwtTokenService = Substitute.For<IJwtTokenService>();
        _authRepository = Substitute.For<IAuthRepository>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();

        _sut = new RefreshTokenCommandHandler(_identityService, _jwtTokenService, _authRepository, _dateTimeProvider);
    }

    [Fact]
    public async Task HandleAsync_WhenUserIdNotExtractedFromToken_ReturnsFailure()
    {
        _jwtTokenService.GetUserIdFromAccessToken("bad-access-token").Returns((string?)null);

        var result = await _sut.HandleAsync(new RefreshTokenCommand("bad-access-token", "any-refresh"));

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task HandleAsync_WhenRefreshTokenNotFound_ReturnsFailure()
    {
        _jwtTokenService.GetUserIdFromAccessToken("valid-access").Returns("user-1");
        _jwtTokenService.HashRefreshToken("raw-refresh").Returns("hashed");
        _authRepository.FindActiveRefreshTokenAsync("user-1", "hashed", Arg.Any<CancellationToken>())
            .Returns((Domain.Auth.RefreshToken?)null);

        var result = await _sut.HandleAsync(new RefreshTokenCommand("valid-access", "raw-refresh"));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task HandleAsync_WhenSuccessful_RevokesOldAndIssuesNewTokens()
    {
        const string userId = "user-1";
        const string oldRefreshRaw = "old-raw-refresh";
        const string oldRefreshHash = "old-hash";
        const string newAccessToken = "new-access-token";
        const string newRefreshRaw = "new-raw-refresh";
        const string newRefreshHash = "new-hash";

        var existingToken = new Domain.Auth.RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = oldRefreshHash,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(5),
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-2)
        };

        _jwtTokenService.GetUserIdFromAccessToken("old-access").Returns(userId);
        _jwtTokenService.HashRefreshToken(oldRefreshRaw).Returns(oldRefreshHash);
        _authRepository.FindActiveRefreshTokenAsync(userId, oldRefreshHash, Arg.Any<CancellationToken>())
            .Returns(existingToken);
        _identityService.GetUserRolesAsync(userId, Arg.Any<CancellationToken>()).Returns(["User"]);
        _jwtTokenService.AccessTokenExpiresInSeconds.Returns(900);
        _jwtTokenService.RefreshTokenExpiresInDays.Returns(7);
        _jwtTokenService.GenerateAccessToken(userId, Arg.Any<string>(), Arg.Any<IEnumerable<string>>())
            .Returns(newAccessToken);
        _jwtTokenService.GenerateRefreshToken().Returns(newRefreshRaw);
        _jwtTokenService.HashRefreshToken(newRefreshRaw).Returns(newRefreshHash);
        _dateTimeProvider.UtcNow.Returns(new DateTime(2026, 6, 12, 0, 0, 0, DateTimeKind.Utc));

        var result = await _sut.HandleAsync(new RefreshTokenCommand("old-access", oldRefreshRaw));

        Assert.True(result.IsSuccess);
        Assert.Equal(newAccessToken, result.Value!.AccessToken);
        Assert.Equal(newRefreshRaw, result.Value.RefreshToken);
        // Old token must be revoked
        Assert.NotNull(existingToken.RevokedAt);
        await _authRepository.Received(1).AddRefreshTokenAsync(
            Arg.Is<Domain.Auth.RefreshToken>(t => t.TokenHash == newRefreshHash && t.UserId == userId),
            Arg.Any<CancellationToken>());
        await _authRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenSuccessful_EmailComesFromIdentityService()
    {
        const string userId = "user-1";
        var existingToken = new Domain.Auth.RefreshToken
        {
            Id = Guid.NewGuid(), UserId = userId, TokenHash = "hash",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(5),
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        _jwtTokenService.GetUserIdFromAccessToken(Arg.Any<string>()).Returns(userId);
        _jwtTokenService.HashRefreshToken(Arg.Any<string>()).Returns("hash");
        _authRepository.FindActiveRefreshTokenAsync(userId, "hash", Arg.Any<CancellationToken>())
            .Returns(existingToken);
        _identityService.GetUserEmailAsync(userId, Arg.Any<CancellationToken>()).Returns("user@example.com");
        _identityService.GetUserRolesAsync(userId, Arg.Any<CancellationToken>()).Returns(new List<string>());
        _jwtTokenService.AccessTokenExpiresInSeconds.Returns(900);
        _jwtTokenService.RefreshTokenExpiresInDays.Returns(7);
        _jwtTokenService.GenerateAccessToken(userId, "user@example.com", Arg.Any<IEnumerable<string>>())
            .Returns("new-token");
        _jwtTokenService.GenerateRefreshToken().Returns("new-raw");
        _jwtTokenService.HashRefreshToken("new-raw").Returns("new-hash");
        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);

        var result = await _sut.HandleAsync(new RefreshTokenCommand("old-access", "old-raw"));

        Assert.True(result.IsSuccess);
        _jwtTokenService.Received(1).GenerateAccessToken(userId, "user@example.com", Arg.Any<IEnumerable<string>>());
    }
}
