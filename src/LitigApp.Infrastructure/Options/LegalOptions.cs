using System.ComponentModel.DataAnnotations;

namespace LitigApp.Infrastructure.Options;

public sealed class LegalOptions
{
    public const string SectionName = "Legal";

    [Required] public string TermsVersion { get; set; } = null!;
    [Required] public string PrivacyVersion { get; set; } = null!;
    [Required] public string DataProtectionEmail { get; set; } = null!;
}
