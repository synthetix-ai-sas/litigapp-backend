namespace LitigApp.Domain.Auth;

public sealed class RefreshToken
{
    public Guid Id { get; init; }
    public string UserId { get; init; } = default!;
    public string TokenHash { get; init; } = default!;
    public DateTimeOffset ExpiresAt { get; init; }
    public DateTimeOffset? RevokedAt { get; set; }
    public DateTimeOffset CreatedAt { get; init; }

    public bool IsActive => RevokedAt is null && DateTimeOffset.UtcNow < ExpiresAt;
}
