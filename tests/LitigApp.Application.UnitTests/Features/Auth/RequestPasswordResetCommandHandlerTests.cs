using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Auth;
using LitigApp.Application.Features.Auth.Commands.RequestPasswordReset;
using Microsoft.Extensions.Options;
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

        var renderer = Substitute.For<IEmailTemplateRenderer>();
        renderer.Render(Arg.Any<EmailTemplate>(), Arg.Any<IReadOnlyDictionary<string, object?>>())
            .Returns("<html>reset</html>");

        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        var opts = Options.Create(new AuthOptions { FrontendBaseUrl = "https://test.litigapp.co" });

        _sut = new RequestPasswordResetCommandHandler(_identityService, _emailSender, renderer, clock, opts);
    }

    [Fact]
    public async Task HandleAsync_WhenUserDoesNotExist_ReturnsSuccessWithoutSendingEmail()
    {
        _identityService
            .GeneratePasswordResetAsync("ghost@example.com", Arg.Any<CancellationToken>())
            .Returns((PasswordResetData?)null);

        var result = await _sut.HandleAsync(new RequestPasswordResetCommand("ghost@example.com"));

        Assert.True(result.IsSuccess);
        await _emailSender.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<IReadOnlyList<EmailAttachment>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenUserExists_SendsResetEmail()
    {
        _identityService
            .GeneratePasswordResetAsync("user@example.com", Arg.Any<CancellationToken>())
            .Returns(new PasswordResetData("user-id-1", "Test User", "reset-token-abc"));
        _emailSender
            .SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<string?>(), Arg.Any<IReadOnlyList<EmailAttachment>?>(), Arg.Any<CancellationToken>())
            .Returns(LitigApp.Domain.Common.Result<string>.Success("ok"));

        var result = await _sut.HandleAsync(new RequestPasswordResetCommand("user@example.com"));

        Assert.True(result.IsSuccess);
        await _emailSender.Received(1).SendAsync(
            "user@example.com",
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<IReadOnlyList<EmailAttachment>?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_AlwaysReturnsSuccess_DoesNotLeakUserExistence()
    {
        _identityService
            .GeneratePasswordResetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((PasswordResetData?)null);

        var result = await _sut.HandleAsync(new RequestPasswordResetCommand("anyone@example.com"));

        Assert.True(result.IsSuccess);
    }
}
