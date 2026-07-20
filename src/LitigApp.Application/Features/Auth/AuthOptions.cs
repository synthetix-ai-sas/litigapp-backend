using System.ComponentModel.DataAnnotations;

namespace LitigApp.Application.Features.Auth;

/// <summary>Bound from section "Auth".</summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>Lifetime of the password reset token in minutes.</summary>
    [Range(1, 60)]
    public int TokenLifespanMinutes { get; init; } = 15;
}
