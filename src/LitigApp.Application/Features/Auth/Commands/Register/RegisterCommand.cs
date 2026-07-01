using LitigApp.Application.Common.Abstractions;

namespace LitigApp.Application.Features.Auth.Commands.Register;

public record RegisterCommand(
    string Email,
    string Password,
    string FullName,
    string? WhatsAppPhone,
    bool AcceptedTerms,
    bool AcceptedPrivacy,
    string? IpAddress,
    string TermsVersion,
    string PrivacyVersion) : ICommand<AuthTokensResponse>;
