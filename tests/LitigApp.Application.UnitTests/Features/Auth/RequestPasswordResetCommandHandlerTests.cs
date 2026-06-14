using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Auth.Commands.RequestPasswordReset;
using NSubstitute;

namespace LitigApp.Application.UnitTests.Features.Auth;

public class RequestPasswordResetCommandHandlerTests
{
    private readonly IIdentityService _identityService;
    private readonly IEmailSender _emailSender;
    private readonly RequestPasswordResetCommandHandler _sut;

    public RequestPasswordResetCommandHandlerTests()
    {
        _identityService = Substitute.For<IIdentityService>();
        _emailSender = Substitute.For<IEmailSender>();

        _sut = new RequestPasswordResetCommandHandler(_identityService, _emailSender);
    }

    [Fact]
    public async Task HandleAsync_WhenUserDoesNotExist_ReturnsSuccessWithoutSendingEmail()
    {
        _identityService.GetPasswordResetTokenAsync("ghost@example.com", Arg.Any<CancellationToken>())
            .Returns((string?)null);

        var result = await _sut.HandleAsync(new RequestPasswordResetCommand("ghost@example.com"));

        Assert.True(result.IsSuccess);
        await _emailSender.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenUserExists_SendsResetEmail()
    {
        _identityService.GetPasswordResetTokenAsync("user@example.com", Arg.Any<CancellationToken>())
            .Returns("reset-token-abc");
        _emailSender.SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Domain.Common.Result<string>.Success("ok"));

        var result = await _sut.HandleAsync(new RequestPasswordResetCommand("user@example.com"));

        Assert.True(result.IsSuccess);
        await _emailSender.Received(1).SendAsync(
            "user@example.com",
            Arg.Any<string>(),
            Arg.Is<string>(body => body.Contains("reset-token-abc")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_AlwaysReturnsSuccess_DoesNotLeakUserExistence()
    {
        _identityService.GetPasswordResetTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((string?)null);

        var result = await _sut.HandleAsync(new RequestPasswordResetCommand("anyone@example.com"));

        Assert.True(result.IsSuccess);
    }
}
