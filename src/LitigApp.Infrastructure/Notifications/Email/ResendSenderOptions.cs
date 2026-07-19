using System.ComponentModel.DataAnnotations;

namespace LitigApp.Infrastructure.Notifications.Email;

/// <summary>
/// Configuration for <see cref="ResendEmailSender"/>. Bound from section "Resend".
/// The Resend package's own <c>ResendClientOptions.ApiToken</c> is configured separately,
/// straight from the <c>RESEND_APITOKEN</c> env var (blueprint §10.4) — not through this class.
/// </summary>
public sealed class ResendSenderOptions
{
    public const string SectionName = "Resend";

    /// <summary>Verified sending domain address, e.g. contac@notifications.synthetixaisas.com.</summary>
    [Required]
    public string FromAddress { get; init; } = string.Empty;

    [Required]
    public string FromName { get; init; } = "LitigApp";

    /// <summary>
    /// Dev/staging only (env var): when set, EVERY email is redirected here instead of the
    /// real recipient, so notifications can be tested without spamming actual lawyers.
    /// </summary>
    public string? DevRedirectTo { get; init; }
}
