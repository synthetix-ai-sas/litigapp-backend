namespace LitigApp.Domain.Auth;

public sealed class LegalAcceptance
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = null!;
    /// <summary>"terms" | "privacy"</summary>
    public string DocumentType { get; set; } = null!;
    public string DocumentVersion { get; set; } = null!;
    public DateTimeOffset AcceptedAt { get; set; }
    public string? IpAddress { get; set; }
}
