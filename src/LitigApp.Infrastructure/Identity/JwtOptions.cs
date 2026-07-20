using System.ComponentModel.DataAnnotations;

namespace LitigApp.Infrastructure.Identity;

public sealed class JwtOptions
{
    public const string SectionName = "Auth:Jwt";

    [Required, MinLength(32)]
    public string Secret { get; init; } = default!;

    [Required]
    public string Issuer { get; init; } = default!;

    [Required]
    public string Audience { get; init; } = default!;

    [Range(1, 1440)]
    public int AccessTokenMinutes { get; init; } = 15;

    [Range(1, 365)]
    public int RefreshTokenDays { get; init; } = 7;
}
