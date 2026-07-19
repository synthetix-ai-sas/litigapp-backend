using LitigApp.Application.Common.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace LitigApp.Infrastructure.Identity;

public sealed class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public IdentityService(UserManager<ApplicationUser> userManager) =>
        _userManager = userManager;

    public async Task<bool> IsEmailRegisteredAsync(string email, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user is not null;
    }

    public async Task<IdentityOperationResult> CreateUserAsync(
        string email, string password, string fullName, string? whatsAppPhone, CancellationToken ct = default)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            WhatsAppPhone = whatsAppPhone,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var error = string.Join("; ", result.Errors.Select(e => e.Description));
            return new IdentityOperationResult(false, null, error);
        }

        return new IdentityOperationResult(true, user.Id, null);
    }

    public async Task<string?> ValidateCredentialsAsync(string email, string password, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
            return null;

        var valid = await _userManager.CheckPasswordAsync(user, password);
        return valid ? user.Id : null;
    }

    public async Task<IReadOnlyList<string>> GetUserRolesAsync(string userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return [];

        var roles = await _userManager.GetRolesAsync(user);
        return [.. roles];
    }

    public async Task<string?> GetUserEmailAsync(string userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user?.Email;
    }

    public async Task<UserProfile?> GetUserProfileAsync(string userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user?.Email is null ? null : new UserProfile(user.Email, user.FullName);
    }

    public async Task<string?> GetPasswordResetTokenAsync(string email, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
            return null;

        return await _userManager.GeneratePasswordResetTokenAsync(user);
    }

    public async Task<IdentityOperationResult> ResetPasswordAsync(
        string email, string resetToken, string newPassword, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
            return new IdentityOperationResult(false, null, "User not found.");

        var result = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);
        if (!result.Succeeded)
        {
            var error = string.Join("; ", result.Errors.Select(e => e.Description));
            return new IdentityOperationResult(false, null, error);
        }

        return new IdentityOperationResult(true, user.Id, null);
    }
}
