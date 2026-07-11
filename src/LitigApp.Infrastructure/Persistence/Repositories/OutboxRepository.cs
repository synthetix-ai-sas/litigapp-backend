using LitigApp.Application.Common.Abstractions;
using LitigApp.Domain.Notifications;
using LitigApp.Infrastructure.Persistence;

namespace LitigApp.Infrastructure.Persistence.Repositories;

internal sealed class OutboxRepository(AppDbContext db) : IOutboxRepository
{
    public async Task InsertAsync(OutboxMessage message, CancellationToken ct)
    {
        await db.OutboxMessages.AddAsync(message, ct);
        await db.SaveChangesAsync(ct);
    }
}
