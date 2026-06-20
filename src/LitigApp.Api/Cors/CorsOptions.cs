using System.ComponentModel.DataAnnotations;

namespace LitigApp.Api.Cors;

public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    [Required, MinLength(1)]
    public string[] AllowedOrigins { get; init; } = [];
}
