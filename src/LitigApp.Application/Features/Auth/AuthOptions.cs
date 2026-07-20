using System.ComponentModel.DataAnnotations;

namespace LitigApp.Application.Features.Auth;

/// <summary>Bound from section "Auth". Used for building password reset links.</summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>Frontend base URL used to build the password reset link.</summary>
    [Required]
    public string FrontendBaseUrl { get; init; } = "https://app.litigapp.co";

    /// <summary>
    /// Lifetime of the password reset token in minutes.
    /// Must match DataProtectionTokenProviderOptions.TokenLifespan configured in Infrastructure.
    /// </summary>
    [Range(5, 1440)]
    public int TokenLifespanMinutes { get; init; } = 60;
}
