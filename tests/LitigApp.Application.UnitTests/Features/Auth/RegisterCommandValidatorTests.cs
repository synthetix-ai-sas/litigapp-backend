using LitigApp.Application.Features.Auth.Commands.Register;

namespace LitigApp.Application.UnitTests.Features.Auth;

public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _sut = new();

    private static RegisterCommand ValidCmd(
        string email = "user@example.com",
        string password = "password123",
        string fullName = "Juan Pérez",
        string? phone = null,
        bool acceptedTerms = true,
        bool acceptedPrivacy = true) =>
        new(email, password, fullName, phone, acceptedTerms, acceptedPrivacy, null, "v1.0", "v1.0");

    [Fact]
    public async Task Valid_full_input_passes()
    {
        var result = await _sut.ValidateAsync(ValidCmd(phone: "+573001234567"));
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Valid_input_without_whatsapp_passes()
    {
        var result = await _sut.ValidateAsync(ValidCmd());
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("notanemail")]
    [InlineData("missing@")]
    public async Task Invalid_email_fails(string email)
    {
        var result = await _sut.ValidateAsync(ValidCmd(email: email));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterCommand.Email));
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("1234567")]
    public async Task Password_shorter_than_8_chars_fails(string password)
    {
        var result = await _sut.ValidateAsync(ValidCmd(password: password));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterCommand.Password));
    }

    [Fact]
    public async Task Empty_fullName_fails()
    {
        var result = await _sut.ValidateAsync(ValidCmd(fullName: ""));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterCommand.FullName));
    }

    [Theory]
    [InlineData("3001234567")]
    [InlineData("+13001234567")]
    [InlineData("+5730012345")]
    [InlineData("+57300123456789")]
    public async Task Invalid_whatsapp_phone_fails(string phone)
    {
        var result = await _sut.ValidateAsync(ValidCmd(phone: phone));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterCommand.WhatsAppPhone));
    }

    [Fact]
    public async Task Valid_colombian_e164_phone_passes()
    {
        var result = await _sut.ValidateAsync(ValidCmd(phone: "+573101234567"));
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task AcceptedTerms_false_fails()
    {
        var result = await _sut.ValidateAsync(ValidCmd(acceptedTerms: false));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterCommand.AcceptedTerms));
    }

    [Fact]
    public async Task AcceptedPrivacy_false_fails()
    {
        var result = await _sut.ValidateAsync(ValidCmd(acceptedPrivacy: false));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterCommand.AcceptedPrivacy));
    }
}
