using System.ComponentModel.DataAnnotations;

namespace LitigApp.Application.Common.Options;

/// <summary>Cross-cutting application URLs. Single source of truth for the frontend base URL.</summary>
public sealed class AppOptions
{
    public const string SectionName = "App";

    [Required]
    public string FrontendBaseUrl { get; init; } = "https://app.litigapp.co";
}
