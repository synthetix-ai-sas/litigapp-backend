using LitigApp.Application.Common.Abstractions;
using LitigApp.Domain.Auth;
using Microsoft.EntityFrameworkCore;

namespace LitigApp.Infrastructure.Persistence.Repositories;

public sealed class AuthRepository : IAuthRepository
{
    private readonly AppDbContext _db;

    public AuthRepository(AppDbContext db) => _db = db;

    public async Task AddRefreshTokenAsync(RefreshToken token, CancellationToken ct = default) =>
        await _db.RefreshTokens.AddAsync(token, ct);

    public Task<RefreshToken?> FindActiveRefreshTokenAsync(string userId, string tokenHash, CancellationToken ct = default) =>
        _db.RefreshTokens
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.TokenHash == tokenHash && t.RevokedAt == null)
            .FirstOrDefaultAsync(ct);

    public async Task RevokeAllUserRefreshTokensAsync(string userId, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);
    }

    public async Task AddLegalAcceptanceAsync(LegalAcceptance acceptance, CancellationToken ct = default) =>
        await _db.LegalAcceptances.AddAsync(acceptance, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
