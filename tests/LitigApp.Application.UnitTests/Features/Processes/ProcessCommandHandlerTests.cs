using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Processes;
using LitigApp.Application.Features.Processes.Commands.CreateFromWizard;
using LitigApp.Application.Features.Processes.Commands.MarkAttended;
using LitigApp.Application.Features.Processes.Commands.SoftDelete;
using LitigApp.Application.Features.Processes.Services;
using LitigApp.Domain.Catalog;
using LitigApp.Domain.Processes;
using NSubstitute;

namespace LitigApp.Application.UnitTests.Features.Processes;

public class FileNumberRulesTests
{
    [Fact]
    public void Compose_PadsConsecutiveWithTrailingZeros()
    {
        var result = FileNumberRules.Compose("170014003010", 2024, "192");

        Assert.True(result.IsSuccess);
        Assert.Equal("17001400301020241920000", result.Value);
        Assert.Equal(23, result.Value!.Length);
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345678")] // > 7 digits
    [InlineData("19A")]      // non-digit
    public void Compose_InvalidConsecutive_Fails(string consecutive)
    {
        var result = FileNumberRules.Compose("170014003010", 2024, consecutive);

        Assert.False(result.IsSuccess);
        Assert.Equal(ProcessErrorCodes.InvalidConsecutive, result.Error);
    }
}

public class CreateProcessFromWizardHandlerTests
{
    private readonly IProcessRepository _repo = Substitute.For<IProcessRepository>();
    private readonly ProcessCreationService _creation = new(
        Substitute.For<IProcessRepository>(),
        Substitute.For<IRamaJudicialClient>(),
        Substitute.For<IProcessReader>(),
        Substitute.For<IDateTimeProvider>(),
        Substitute.For<IPartialFetchScheduler>(),
        Substitute.For<ICurrentUserService>());

    [Fact]
    public async Task HandleAsync_UnknownCourt_ReturnsCourtNotFound()
    {
        _repo.FindCourtAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Court?)null);

        var handler = new CreateProcessFromWizardHandler(_repo, _creation);
        var result = await handler.HandleAsync(
            new CreateProcessFromWizardCommand("17001", Guid.NewGuid(), 2024, "192"));

        Assert.False(result.IsSuccess);
        Assert.Equal(ProcessErrorCodes.CourtNotFound, result.Error);
    }

    [Fact]
    public async Task HandleAsync_CourtBelongsToDifferentCity_ReturnsCourtNotFound()
    {
        var courtId = Guid.NewGuid();
        _repo.FindCourtAsync(courtId, Arg.Any<CancellationToken>())
            .Returns(new Court { Id = courtId, OfficialCode = "170014003010", CityId = "66001" });

        var handler = new CreateProcessFromWizardHandler(_repo, _creation);
        var result = await handler.HandleAsync(
            new CreateProcessFromWizardCommand("17001", courtId, 2024, "192"));

        Assert.False(result.IsSuccess);
        Assert.Equal(ProcessErrorCodes.CourtNotFound, result.Error);
    }
}

public class MarkAttendedHandlerTests
{
    private readonly IProcessRepository _repo = Substitute.For<IProcessRepository>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly ICurrentUserService _user = Substitute.For<ICurrentUserService>();

    public MarkAttendedHandlerTests()
    {
        _user.UserId.Returns("u1");
        _clock.UtcNow.Returns(DateTime.UtcNow);
    }

    [Fact]
    public async Task HandleAsync_NotOwned_ReturnsNotFound()
    {
        _repo.GetOwnedAsync(Arg.Any<Guid>(), "u1", Arg.Any<CancellationToken>()).Returns((Process?)null);

        var handler = new MarkAttendedHandler(_repo, _clock, _user);
        var result = await handler.HandleAsync(new MarkAttendedCommand(Guid.NewGuid()));

        Assert.False(result.IsSuccess);
        Assert.Equal(ProcessErrorCodes.ProcessNotFound, result.Error);
    }

    [Fact]
    public async Task HandleAsync_Unattended_SetsAttendedAndSaves()
    {
        var process = new Process { Id = Guid.NewGuid(), UserId = "u1", Attended = false };
        _repo.GetOwnedAsync(process.Id, "u1", Arg.Any<CancellationToken>()).Returns(process);

        var handler = new MarkAttendedHandler(_repo, _clock, _user);
        var result = await handler.HandleAsync(new MarkAttendedCommand(process.Id));

        Assert.True(result.IsSuccess);
        Assert.True(process.Attended);
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_AlreadyAttended_IsIdempotentNoSave()
    {
        var process = new Process { Id = Guid.NewGuid(), UserId = "u1", Attended = true };
        _repo.GetOwnedAsync(process.Id, "u1", Arg.Any<CancellationToken>()).Returns(process);

        var handler = new MarkAttendedHandler(_repo, _clock, _user);
        var result = await handler.HandleAsync(new MarkAttendedCommand(process.Id));

        Assert.True(result.IsSuccess);
        await _repo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

public class SoftDeleteProcessHandlerTests
{
    private readonly IProcessRepository _repo = Substitute.For<IProcessRepository>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly ICurrentUserService _user = Substitute.For<ICurrentUserService>();

    public SoftDeleteProcessHandlerTests()
    {
        _user.UserId.Returns("u1");
        _clock.UtcNow.Returns(DateTime.UtcNow);
    }

    [Fact]
    public async Task HandleAsync_NotOwned_ReturnsNotFound()
    {
        _repo.GetOwnedAsync(Arg.Any<Guid>(), "u1", Arg.Any<CancellationToken>()).Returns((Process?)null);

        var handler = new SoftDeleteProcessHandler(_repo, _clock, _user);
        var result = await handler.HandleAsync(new SoftDeleteProcessCommand(Guid.NewGuid()));

        Assert.False(result.IsSuccess);
        Assert.Equal(ProcessErrorCodes.ProcessNotFound, result.Error);
    }

    [Fact]
    public async Task HandleAsync_Active_SetsInactiveAndSaves()
    {
        var process = new Process { Id = Guid.NewGuid(), UserId = "u1", IsActive = true };
        _repo.GetOwnedAsync(process.Id, "u1", Arg.Any<CancellationToken>()).Returns(process);

        var handler = new SoftDeleteProcessHandler(_repo, _clock, _user);
        var result = await handler.HandleAsync(new SoftDeleteProcessCommand(process.Id));

        Assert.True(result.IsSuccess);
        Assert.False(process.IsActive);
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
