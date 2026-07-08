using LitigApp.Domain.Auth;

namespace LitigApp.Application.Common.Abstractions;

public interface IAuthRepository
{
    Task AddRefreshTokenAsync(RefreshToken token, CancellationToken ct = default);
    Task<RefreshToken?> FindActiveRefreshTokenAsync(string userId, string tokenHash, CancellationToken ct = default);
    Task RevokeAllUserRefreshTokensAsync(string userId, CancellationToken ct = default);
    Task AddLegalAcceptanceAsync(LegalAcceptance acceptance, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
