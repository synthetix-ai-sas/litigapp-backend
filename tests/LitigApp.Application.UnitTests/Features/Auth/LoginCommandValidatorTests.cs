using LitigApp.Application.Features.Auth.Commands.Login;

namespace LitigApp.Application.UnitTests.Features.Auth;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _sut = new();

    [Fact]
    public async Task Valid_credentials_pass()
    {
        var result = await _sut.ValidateAsync(new LoginCommand("user@example.com", "password123"));
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("notanemail")]
    public async Task Invalid_email_fails(string email)
    {
        var result = await _sut.ValidateAsync(new LoginCommand(email, "password123"));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(LoginCommand.Email));
    }

    [Fact]
    public async Task Empty_password_fails()
    {
        var result = await _sut.ValidateAsync(new LoginCommand("user@example.com", ""));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(LoginCommand.Password));
    }
}
