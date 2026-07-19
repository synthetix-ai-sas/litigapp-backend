using LitigApp.Application.Common.Abstractions;
using LitigApp.Domain.Notifications;
using Microsoft.EntityFrameworkCore;

namespace LitigApp.Infrastructure.Persistence.Repositories;

internal sealed class NotificationLogRepository(AppDbContext db) : INotificationLogRepository
{
    public Task<DateTimeOffset?> GetLastEmailSentAtAsync(string userId, CancellationToken ct) =>
        db.NotificationLogs.AsNoTracking()
            .Where(l => l.UserId == userId && l.Channel == "email")
            .OrderByDescending(l => l.SentAt)
            .Select(l => (DateTimeOffset?)l.SentAt)
            .FirstOrDefaultAsync(ct);

    public async Task InsertAsync(NotificationLog log, CancellationToken ct)
    {
        await db.NotificationLogs.AddAsync(log, ct);
        await db.SaveChangesAsync(ct);
    }
}
