using LitigApp.Application.Common.Abstractions;
using LitigApp.Domain.Notifications;
using LitigApp.Jobs.ProcessSyncJobs;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace LitigApp.Jobs.UnitTests;

public class NotificationFallbackSweepJobUnitTests
{
    private readonly IOutboxRepository _outboxRepo = Substitute.For<IOutboxRepository>();
    private readonly INotificationDispatchService _dispatchService = Substitute.For<INotificationDispatchService>();

    private NotificationFallbackSweepJob CreateSut() =>
        new(_outboxRepo, _dispatchService, NullLogger<NotificationFallbackSweepJob>.Instance);

    [Fact]
    public async Task NoOrphans_DoesNotDispatch()
    {
        _outboxRepo.GetPendingOlderThanAsync(Arg.Any<TimeSpan>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        await CreateSut().RunAsync();

        await _dispatchService.DidNotReceive().DispatchAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HasOrphans_DispatchesEachOne()
    {
        var orphans = new List<OutboxMessage>
        {
            new() { Id = Guid.NewGuid(), UserId = "u1", EventType = "UserProcessesUpdated", Channel = "email", Payload = "{}" },
            new() { Id = Guid.NewGuid(), UserId = "u2", EventType = "ImportComplete", Channel = "email", Payload = "{}" },
        };
        _outboxRepo.GetPendingOlderThanAsync(Arg.Any<TimeSpan>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(orphans);

        await CreateSut().RunAsync();

        await _dispatchService.Received(1).DispatchAsync(orphans[0], Arg.Any<CancellationToken>());
        await _dispatchService.Received(1).DispatchAsync(orphans[1], Arg.Any<CancellationToken>());
    }
}
