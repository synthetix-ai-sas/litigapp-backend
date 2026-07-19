using LitigApp.Application.Common.Abstractions;
using LitigApp.Domain.Notifications;
using LitigApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LitigApp.Infrastructure.Persistence.Repositories;

internal sealed class OutboxRepository(AppDbContext db) : IOutboxRepository
{
    public async Task InsertAsync(OutboxMessage message, CancellationToken ct)
    {
        await db.OutboxMessages.AddAsync(message, ct);
        await db.SaveChangesAsync(ct);
    }

    public Task<OutboxMessage?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.OutboxMessages.FirstOrDefaultAsync(m => m.Id == id, ct);

    public Task<List<OutboxMessage>> GetPendingOlderThanAsync(TimeSpan minAge, int batchSize, CancellationToken ct)
    {
        var cutoff = DateTimeOffset.UtcNow - minAge;
        return db.OutboxMessages
            .Where(m => (m.Status == "pending" || m.Status == "processing") && m.CreatedAt < cutoff)
            .OrderBy(m => m.CreatedAt)
            .Take(batchSize)
            .ToListAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
