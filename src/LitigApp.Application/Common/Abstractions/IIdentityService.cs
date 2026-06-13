namespace LitigApp.Application.Common.Abstractions;

public interface IIdentityService
{
    Task<bool> IsEmailRegisteredAsync(string email, CancellationToken ct = default);
    Task<IdentityOperationResult> CreateUserAsync(
        string email, string password, string fullName, string? whatsAppPhone, CancellationToken ct = default);
    Task<string?> ValidateCredentialsAsync(string email, string password, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetUserRolesAsync(string userId, CancellationToken ct = default);
}

public record IdentityOperationResult(bool Succeeded, string? UserId, string? Error);
