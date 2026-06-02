namespace LitigApp.Domain.Notifications;

public class NotificationLog
{
    public Guid Id { get; set; }

    /// <summary>FK to OutboxMessage.Id — nullable (SET NULL on delete).</summary>
    public Guid? OutboxId { get; set; }

    /// <summary>FK to AspNetUsers.Id (text).</summary>
    public string UserId { get; set; } = string.Empty;

    public string EventType { get; set; } = string.Empty;

    /// <summary>'email' | 'whatsapp'</summary>
    public string Channel { get; set; } = string.Empty;

    /// <summary>Array of Process IDs included in this digest (uuid[]).</summary>
    public Guid[] ProcessIds { get; set; } = [];

    public string? ProviderMessageId { get; set; }

    /// <summary>'delivered' | 'bounced' | 'failed'</summary>
    public string Status { get; set; } = string.Empty;

    public DateTimeOffset SentAt { get; set; }

    /// <summary>Raw JSON response from email provider (jsonb).</summary>
    public string? RawResponse { get; set; }

    // Navigation
    public OutboxMessage? Outbox { get; set; }
}
