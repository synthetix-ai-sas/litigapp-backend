using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Notifications.Dtos;
using LitigApp.Domain.Notifications;
using LitigApp.Jobs.ProcessSyncJobs;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace LitigApp.Jobs.UnitTests;

public class DispatchUserNotificationsJobUnitTests
{
    private static readonly DateTime Now = new(2026, 6, 22, 12, 0, 0, DateTimeKind.Utc);

    private readonly IProcessRepository _processRepo = Substitute.For<IProcessRepository>();
    private readonly INotificationLogRepository _logRepo = Substitute.For<INotificationLogRepository>();
    private readonly IOutboxRepository _outboxRepo = Substitute.For<IOutboxRepository>();
    private readonly INotificationDispatchService _dispatchService = Substitute.For<INotificationDispatchService>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    public DispatchUserNotificationsJobUnitTests() => _clock.UtcNow.Returns(Now);

    private DispatchUserNotificationsJob CreateSut() => new(
        _processRepo, _logRepo, _outboxRepo, _dispatchService, _clock,
        NullLogger<DispatchUserNotificationsJob>.Instance);

    [Fact]
    public async Task NoChangedProcesses_DoesNotInsertOutbox_DoesNotDispatch()
    {
        _logRepo.GetLastEmailSentAtAsync("user-1", Arg.Any<CancellationToken>()).Returns((DateTimeOffset?)null);
        _processRepo.GetChangedSinceAsync("user-1", Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);

        await CreateSut().RunAsync("user-1");

        await _outboxRepo.DidNotReceive().InsertAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>());
        await _dispatchService.DidNotReceive().DispatchAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HasChangedProcesses_InsertsOutbox_ThenDispatches()
    {
        _logRepo.GetLastEmailSentAtAsync("user-1", Arg.Any<CancellationToken>()).Returns((DateTimeOffset?)null);
        var changed = new List<ChangedProcessDto>
        {
            new(Guid.NewGuid(), "17001400301020240019200", new DateTimeOffset(Now), "Fijacion estado", "nota"),
        };
        _processRepo.GetChangedSinceAsync("user-1", Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(changed);

        await CreateSut().RunAsync("user-1");

        await _outboxRepo.Received(1).InsertAsync(
            Arg.Is<OutboxMessage>(m =>
                m.UserId == "user-1" && m.EventType == "UserProcessesUpdated" && m.Channel == "email" &&
                m.Status == "pending" && m.Payload.Contains("17001400301020240019200")),
            Arg.Any<CancellationToken>());
        await _dispatchService.Received(1).DispatchAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NeverNotified_UsesLast24Hours_NotAllTime()
    {
        _logRepo.GetLastEmailSentAtAsync("user-1", Arg.Any<CancellationToken>()).Returns((DateTimeOffset?)null);
        _processRepo.GetChangedSinceAsync("user-1", Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);

        await CreateSut().RunAsync("user-1");

        await _processRepo.Received(1).GetChangedSinceAsync(
            "user-1", new DateTimeOffset(Now).AddDays(-1), Arg.Any<CancellationToken>());
    }
}
