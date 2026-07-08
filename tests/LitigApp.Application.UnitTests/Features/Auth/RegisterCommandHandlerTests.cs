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

    private static RegisterCommand ValidCmd(
        string email = "new@example.com",
        string password = "password123",
        string fullName = "Juan Pérez",
        string? phone = null,
        bool acceptedTerms = true,
        bool acceptedPrivacy = true) =>
        new(email, password, fullName, phone, acceptedTerms, acceptedPrivacy, "127.0.0.1", "v1.0", "v1.0");

    [Fact]
    public async Task HandleAsync_WhenLegalNotAccepted_ReturnsFailureWithoutCreatingUser()
    {
        var cmd = ValidCmd(acceptedTerms: false);

        var result = await _sut.HandleAsync(cmd);

        Assert.False(result.IsSuccess);
        Assert.Equal("LEGAL_NOT_ACCEPTED", result.Error);
        await _identityService.DidNotReceive().IsEmailRegisteredAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _identityService.DidNotReceive().CreateUserAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenEmailAlreadyTaken_ReturnsFailure()
    {
        _identityService.IsEmailRegisteredAsync("taken@example.com", Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _sut.HandleAsync(ValidCmd(email: "taken@example.com"));

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

        var result = await _sut.HandleAsync(ValidCmd(password: "weak"));

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

        var result = await _sut.HandleAsync(ValidCmd(phone: "+573001234567"));

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
    public async Task HandleAsync_WhenSuccessful_PersistsTwoLegalAcceptanceRows()
    {
        const string userId = "user-abc";
        _identityService.IsEmailRegisteredAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _identityService.CreateUserAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new IdentityOperationResult(true, userId, null));
        _identityService.GetUserRolesAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<string> { "User" });
        _jwtTokenService.GenerateAccessToken(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IEnumerable<string>>()).Returns("tok");
        _jwtTokenService.GenerateRefreshToken().Returns("raw");
        _jwtTokenService.HashRefreshToken(Arg.Any<string>()).Returns("hash");
        _dateTimeProvider.UtcNow.Returns(new DateTime(2026, 6, 12, 0, 0, 0, DateTimeKind.Utc));

        await _sut.HandleAsync(ValidCmd());

        await _authRepository.Received(1).AddLegalAcceptanceAsync(
            Arg.Is<Domain.Auth.LegalAcceptance>(la =>
                la.UserId == userId &&
                la.DocumentType == "terms" &&
                la.DocumentVersion == "v1.0"),
            Arg.Any<CancellationToken>());

        await _authRepository.Received(1).AddLegalAcceptanceAsync(
            Arg.Is<Domain.Auth.LegalAcceptance>(la =>
                la.UserId == userId &&
                la.DocumentType == "privacy" &&
                la.DocumentVersion == "v1.0"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenSuccessful_DoesNotPersistTokenIfCreateFails()
    {
        _identityService.IsEmailRegisteredAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _identityService.CreateUserAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new IdentityOperationResult(false, null, "Error."));

        await _sut.HandleAsync(ValidCmd());

        await _authRepository.DidNotReceive().AddRefreshTokenAsync(
            Arg.Any<Domain.Auth.RefreshToken>(), Arg.Any<CancellationToken>());
    }
}
