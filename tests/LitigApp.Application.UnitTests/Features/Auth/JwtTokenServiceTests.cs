using LitigApp.Infrastructure.Identity;
using Microsoft.Extensions.Options;

namespace LitigApp.Application.UnitTests.Features.Auth;

public class JwtTokenServiceTests
{
    private static JwtTokenService CreateService(int accessTokenMinutes = 15) =>
        new(Options.Create(new JwtOptions
        {
            Secret = "test-secret-that-is-at-least-32-characters-long!",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            AccessTokenMinutes = accessTokenMinutes,
            RefreshTokenDays = 7
        }));

    [Fact]
    public void GenerateAccessToken_ReturnsNonEmptyJwt()
    {
        var svc = CreateService();
        var token = svc.GenerateAccessToken("user-1", "test@example.com", ["User"]);
        Assert.NotEmpty(token);
        Assert.Equal(3, token.Split('.').Length); // header.payload.signature
    }

    [Fact]
    public void GenerateAccessToken_SubClaimMatchesUserId()
    {
        var svc = CreateService();
        var token = svc.GenerateAccessToken("user-abc", "test@example.com", []);
        var userId = svc.GetUserIdFromAccessToken(token);
        Assert.Equal("user-abc", userId);
    }

    [Fact]
    public void GenerateRefreshToken_Returns64ByteBase64String()
    {
        var svc = CreateService();
        var token = svc.GenerateRefreshToken();
        Assert.NotEmpty(token);
        var bytes = Convert.FromBase64String(token);
        Assert.Equal(64, bytes.Length);
    }

    [Fact]
    public void GenerateRefreshToken_ProducesDifferentTokensEachTime()
    {
        var svc = CreateService();
        var t1 = svc.GenerateRefreshToken();
        var t2 = svc.GenerateRefreshToken();
        Assert.NotEqual(t1, t2);
    }

    [Fact]
    public void HashRefreshToken_IsDeterministic()
    {
        var svc = CreateService();
        var h1 = svc.HashRefreshToken("some-token");
        var h2 = svc.HashRefreshToken("some-token");
        Assert.Equal(h1, h2);
    }

    [Fact]
    public void HashRefreshToken_DifferentInputsDifferentHashes()
    {
        var svc = CreateService();
        Assert.NotEqual(
            svc.HashRefreshToken("token-a"),
            svc.HashRefreshToken("token-b"));
    }

    [Fact]
    public void ValidateRefreshToken_CorrectPairReturnsTrue()
    {
        var svc = CreateService();
        var raw = svc.GenerateRefreshToken();
        var hash = svc.HashRefreshToken(raw);
        Assert.True(svc.ValidateRefreshToken(raw, hash));
    }

    [Fact]
    public void ValidateRefreshToken_WrongTokenReturnsFalse()
    {
        var svc = CreateService();
        var hash = svc.HashRefreshToken("correct-token");
        Assert.False(svc.ValidateRefreshToken("wrong-token", hash));
    }

    [Fact]
    public void GetUserIdFromAccessToken_ExpiredTokenStillReturnsUserId()
    {
        // accessTokenMinutes=-1 generates an already-expired token
        var svc = CreateService(accessTokenMinutes: -1);
        var token = svc.GenerateAccessToken("user-expired", "exp@example.com", []);
        var userId = svc.GetUserIdFromAccessToken(token);
        Assert.Equal("user-expired", userId);
    }

    [Fact]
    public void GetUserIdFromAccessToken_InvalidTokenReturnsNull()
    {
        var svc = CreateService();
        Assert.Null(svc.GetUserIdFromAccessToken("not.a.valid.jwt"));
    }
}
