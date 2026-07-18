using System.ComponentModel.DataAnnotations;

namespace LitigApp.Application.Features.Notifications;

/// <summary>Bound from section "Notifications" (blueprint §10.4).</summary>
public sealed class NotificationsOptions
{
    public const string SectionName = "Notifications";

    /// <summary>Visible rows in the digest before "y N procesos más" (default 5).</summary>
    [Range(1, 50)]
    public int DigestMaxRows { get; init; } = 5;

    /// <summary>Frontend base URL used to build the CTA links (e.g. /novelties, /processes).</summary>
    [Required]
    public string AppBaseUrl { get; init; } = "https://app.litigapp.co";
}
