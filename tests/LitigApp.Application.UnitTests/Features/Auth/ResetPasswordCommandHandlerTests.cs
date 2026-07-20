using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Auth.Commands.ResetPassword;
using NSubstitute;

namespace LitigApp.Application.UnitTests.Features.Auth;

public class ResetPasswordCommandHandlerTests
{
    private readonly IIdentityService _identityService;
    private readonly IAuthRepository _authRepository;
    private readonly ResetPasswordCommandHandler _sut;

    public ResetPasswordCommandHandlerTests()
    {
        _identityService = Substitute.For<IIdentityService>();
        _authRepository = Substitute.For<IAuthRepository>();
        _sut = new ResetPasswordCommandHandler(_identityService, _authRepository);
    }

    [Fact]
    public async Task HandleAsync_WhenResetSucceeds_ReturnsSuccess_AndRevokesRefreshTokens()
    {
        _identityService
            .ResetPasswordByUserIdAsync("user-id-1", "valid-token", "NewPassword1", Arg.Any<CancellationToken>())
            .Returns(new IdentityOperationResult(true, "user-id-1", null));

        var result = await _sut.HandleAsync(new ResetPasswordCommand("user-id-1", "valid-token", "NewPassword1"));

        Assert.True(result.IsSuccess);
        await _authRepository.Received(1)
            .RevokeAllUserRefreshTokensAsync("user-id-1", Arg.Any<CancellationToken>());
        await _authRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenTokenIsInvalid_ReturnsFailure_AndDoesNotRevoke()
    {
        _identityService
            .ResetPasswordByUserIdAsync(Arg.Any<string>(), "bad-token", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdentityOperationResult(false, null, "INVALID_TOKEN"));

        var result = await _sut.HandleAsync(new ResetPasswordCommand("user-id-1", "bad-token", "NewPassword1"));

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_TOKEN", result.Error);
        await _authRepository.DidNotReceive()
            .RevokeAllUserRefreshTokensAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenUserNotFound_ReturnsFailure()
    {
        _identityService
            .ResetPasswordByUserIdAsync("unknown-id", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdentityOperationResult(false, null, "INVALID_TOKEN"));

        var result = await _sut.HandleAsync(new ResetPasswordCommand("unknown-id", "any-token", "NewPassword1"));

        Assert.False(result.IsSuccess);
    }
}
