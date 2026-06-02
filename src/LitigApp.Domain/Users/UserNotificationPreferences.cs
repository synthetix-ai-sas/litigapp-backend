namespace LitigApp.Domain.Users;

public class UserNotificationPreferences
{
    /// <summary>PK = FK to AspNetUsers.Id (text). One row per user.</summary>
    public string UserId { get; set; } = string.Empty;

    public bool EmailEnabled { get; set; } = true;

    /// <summary>Disabled in MVP; prepared for v2 WhatsApp. Default FALSE.</summary>
    public bool WhatsAppEnabled { get; set; }

    public TimeOnly? QuietHoursStart { get; set; }
    public TimeOnly? QuietHoursEnd { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
