using LitigApp.Application.Features.Auth.Commands.Register;

namespace LitigApp.Application.UnitTests.Features.Auth;

public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _sut = new();

    [Fact]
    public async Task Valid_full_input_passes()
    {
        var cmd = new RegisterCommand("user@example.com", "password123", "Juan Pérez", "+573001234567");
        var result = await _sut.ValidateAsync(cmd);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Valid_input_without_whatsapp_passes()
    {
        var cmd = new RegisterCommand("user@example.com", "password123", "Juan Pérez", null);
        var result = await _sut.ValidateAsync(cmd);
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("notanemail")]
    [InlineData("missing@")]
    public async Task Invalid_email_fails(string email)
    {
        var cmd = new RegisterCommand(email, "password123", "Juan Pérez", null);
        var result = await _sut.ValidateAsync(cmd);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterCommand.Email));
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("1234567")]
    public async Task Password_shorter_than_8_chars_fails(string password)
    {
        var cmd = new RegisterCommand("user@example.com", password, "Juan Pérez", null);
        var result = await _sut.ValidateAsync(cmd);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterCommand.Password));
    }

    [Fact]
    public async Task Empty_fullName_fails()
    {
        var cmd = new RegisterCommand("user@example.com", "password123", "", null);
        var result = await _sut.ValidateAsync(cmd);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterCommand.FullName));
    }

    [Theory]
    [InlineData("3001234567")]        // no + prefix
    [InlineData("+13001234567")]      // wrong country code
    [InlineData("+5730012345")]       // too short
    [InlineData("+57300123456789")]   // too long
    public async Task Invalid_whatsapp_phone_fails(string phone)
    {
        var cmd = new RegisterCommand("user@example.com", "password123", "Juan Pérez", phone);
        var result = await _sut.ValidateAsync(cmd);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterCommand.WhatsAppPhone));
    }

    [Fact]
    public async Task Valid_colombian_e164_phone_passes()
    {
        var cmd = new RegisterCommand("user@example.com", "password123", "Juan Pérez", "+573101234567");
        var result = await _sut.ValidateAsync(cmd);
        Assert.True(result.IsValid);
    }
}
