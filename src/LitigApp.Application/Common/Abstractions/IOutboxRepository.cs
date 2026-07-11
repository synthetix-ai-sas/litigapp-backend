using LitigApp.Domain.Notifications;

namespace LitigApp.Application.Common.Abstractions;

public interface IOutboxRepository
{
    Task InsertAsync(OutboxMessage message, CancellationToken ct);
}
