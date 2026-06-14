namespace LitigApp.Application.Common.Abstractions;

public interface IIdentityService
{
    Task<bool> IsEmailRegisteredAsync(string email, CancellationToken ct = default);
    Task<IdentityOperationResult> CreateUserAsync(
        string email, string password, string fullName, string? whatsAppPhone, CancellationToken ct = default);
    Task<string?> ValidateCredentialsAsync(string email, string password, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetUserRolesAsync(string userId, CancellationToken ct = default);
    Task<string?> GetUserEmailAsync(string userId, CancellationToken ct = default);
    Task<string?> GetPasswordResetTokenAsync(string email, CancellationToken ct = default);
    Task<IdentityOperationResult> ResetPasswordAsync(
        string email, string resetToken, string newPassword, CancellationToken ct = default);
}

public record IdentityOperationResult(bool Succeeded, string? UserId, string? Error);
