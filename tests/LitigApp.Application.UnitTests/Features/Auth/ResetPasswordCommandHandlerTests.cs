using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Auth.Commands.ResetPassword;
using NSubstitute;

namespace LitigApp.Application.UnitTests.Features.Auth;

public class ResetPasswordCommandHandlerTests
{
    private readonly IIdentityService _identityService;
    private readonly ResetPasswordCommandHandler _sut;

    public ResetPasswordCommandHandlerTests()
    {
        _identityService = Substitute.For<IIdentityService>();
        _sut = new ResetPasswordCommandHandler(_identityService);
    }

    [Fact]
    public async Task HandleAsync_WhenResetSucceeds_ReturnsSuccess()
    {
        _identityService.ResetPasswordAsync("user@example.com", "valid-token", "NewPassword1", Arg.Any<CancellationToken>())
            .Returns(new IdentityOperationResult(true, null, null));

        var result = await _sut.HandleAsync(new ResetPasswordCommand("user@example.com", "valid-token", "NewPassword1"));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task HandleAsync_WhenResetFails_ReturnsFailure()
    {
        _identityService.ResetPasswordAsync(Arg.Any<string>(), "invalid-token", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdentityOperationResult(false, null, "Invalid token."));

        var result = await _sut.HandleAsync(new ResetPasswordCommand("user@example.com", "invalid-token", "NewPassword1"));

        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid token.", result.Error);
    }

    [Fact]
    public async Task HandleAsync_WhenUserNotFound_ReturnsFailure()
    {
        _identityService.ResetPasswordAsync("ghost@example.com", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdentityOperationResult(false, null, "User not found."));

        var result = await _sut.HandleAsync(new ResetPasswordCommand("ghost@example.com", "any-token", "NewPassword1"));

        Assert.False(result.IsSuccess);
    }
}
