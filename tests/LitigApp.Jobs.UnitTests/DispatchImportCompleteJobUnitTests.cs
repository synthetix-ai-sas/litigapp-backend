using LitigApp.Application.Common.Abstractions;
using LitigApp.Domain.Notifications;
using LitigApp.Jobs.ProcessSyncJobs;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace LitigApp.Jobs.UnitTests;

public class DispatchImportCompleteJobUnitTests
{
    private readonly IOutboxRepository _outboxRepo = Substitute.For<IOutboxRepository>();
    private readonly INotificationDispatchService _dispatchService = Substitute.For<INotificationDispatchService>();

    private DispatchImportCompleteJob CreateSut() =>
        new(_outboxRepo, _dispatchService, NullLogger<DispatchImportCompleteJob>.Instance);

    [Fact]
    public async Task OutboxNotFound_DoesNotDispatch()
    {
        var outboxId = Guid.NewGuid();
        _outboxRepo.GetByIdAsync(outboxId, Arg.Any<CancellationToken>()).Returns((OutboxMessage?)null);

        await CreateSut().RunAsync(outboxId);

        await _dispatchService.DidNotReceive().DispatchAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OutboxFound_Dispatches()
    {
        var outboxId = Guid.NewGuid();
        var message = new OutboxMessage { Id = outboxId, UserId = "user-1", EventType = "ImportComplete", Channel = "email", Payload = "{}" };
        _outboxRepo.GetByIdAsync(outboxId, Arg.Any<CancellationToken>()).Returns(message);

        await CreateSut().RunAsync(outboxId);

        await _dispatchService.Received(1).DispatchAsync(message, Arg.Any<CancellationToken>());
    }
}
