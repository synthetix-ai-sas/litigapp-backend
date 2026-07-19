namespace LitigApp.Application.Common.Abstractions;

public interface IIdentityService
{
    Task<bool> IsEmailRegisteredAsync(string email, CancellationToken ct = default);
    Task<IdentityOperationResult> CreateUserAsync(
        string email, string password, string fullName, string? whatsAppPhone, CancellationToken ct = default);
    Task<string?> ValidateCredentialsAsync(string email, string password, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetUserRolesAsync(string userId, CancellationToken ct = default);
    Task<string?> GetUserEmailAsync(string userId, CancellationToken ct = default);
    Task<UserProfile?> GetUserProfileAsync(string userId, CancellationToken ct = default);
    Task<PasswordResetData?> GeneratePasswordResetAsync(string email, CancellationToken ct = default);
    Task<IdentityOperationResult> ResetPasswordByUserIdAsync(
        string userId, string token, string newPassword, CancellationToken ct = default);
}

public record IdentityOperationResult(bool Succeeded, string? UserId, string? Error);

/// <summary>Minimal profile needed to address an email (notifications).</summary>
public record UserProfile(string Email, string FullName);

/// <summary>Data returned when generating a password reset token — avoids a second DB lookup.</summary>
public record PasswordResetData(string UserId, string FullName, string Token);
