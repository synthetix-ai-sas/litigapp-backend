namespace LitigApp.Domain.Notifications;

/// <summary>
/// Outbox pattern: one row = one notification to one user on one channel.
/// A digest for N changed processes = ONE row with payload.processes=[N items].
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; set; }

    /// <summary>FK to AspNetUsers.Id (text).</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>'UserProcessesUpdated' | 'ImportComplete' | 'WelcomeUser'</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>'email' | 'whatsapp'</summary>
    public string Channel { get; set; } = string.Empty;

    /// <summary>JSON payload (jsonb), shape depends on EventType.</summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>'pending' | 'processing' | 'sent' | 'failed'</summary>
    public string Status { get; set; } = "pending";

    public short Attempts { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
}
