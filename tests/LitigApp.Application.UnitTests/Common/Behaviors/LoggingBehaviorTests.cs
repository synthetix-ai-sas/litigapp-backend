using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Common.Behaviors;
using LitigApp.Domain.Common;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace LitigApp.Application.UnitTests.Common.Behaviors;

// Must be public — NSubstitute (Castle DynamicProxy) needs to create a proxy for ICommandHandler<TestCommand, string>
public sealed record TestCommand(string Value) : ICommand<string>;

public sealed class LoggingBehaviorTests
{

    private readonly ICommandHandler<TestCommand, string> _inner;
    private readonly ILogger<LoggingBehavior<TestCommand, string>> _logger;
    private readonly LoggingBehavior<TestCommand, string> _sut;

    public LoggingBehaviorTests()
    {
        _inner = Substitute.For<ICommandHandler<TestCommand, string>>();
        _logger = Substitute.For<ILogger<LoggingBehavior<TestCommand, string>>>();
        _sut = new LoggingBehavior<TestCommand, string>(_inner, _logger);
    }

    [Fact]
    public async Task HandleAsync_DelegatesCallToInnerHandler()
    {
        var command = new TestCommand("test");
        _inner.HandleAsync(command, Arg.Any<CancellationToken>())
            .Returns(Result<string>.Success("ok"));

        await _sut.HandleAsync(command);

        await _inner.Received(1).HandleAsync(command, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenInnerSucceeds_ReturnsSuccessResult()
    {
        _inner.HandleAsync(Arg.Any<TestCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<string>.Success("result-value"));

        var result = await _sut.HandleAsync(new TestCommand("test"));

        Assert.True(result.IsSuccess);
        Assert.Equal("result-value", result.Value);
    }

    [Fact]
    public async Task HandleAsync_WhenInnerFails_ReturnsFailureResult()
    {
        _inner.HandleAsync(Arg.Any<TestCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<string>.Failure("something went wrong"));

        var result = await _sut.HandleAsync(new TestCommand("test"));

        Assert.False(result.IsSuccess);
        Assert.Equal("something went wrong", result.Error);
    }

    [Fact]
    public async Task HandleAsync_WhenInnerThrows_RethrowsException()
    {
        _inner.HandleAsync(Arg.Any<TestCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("unexpected failure"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.HandleAsync(new TestCommand("test")));
    }

    [Fact]
    public async Task HandleAsync_WhenInnerThrows_DoesNotSwallowOriginalException()
    {
        var expected = new InvalidOperationException("boom");
        _inner.HandleAsync(Arg.Any<TestCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(expected);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.HandleAsync(new TestCommand("test")));

        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task HandleAsync_CallsInnerHandlerExactlyOnce_EvenOnFailureResult()
    {
        _inner.HandleAsync(Arg.Any<TestCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<string>.Failure("error"));

        await _sut.HandleAsync(new TestCommand("test"));

        await _inner.Received(1).HandleAsync(Arg.Any<TestCommand>(), Arg.Any<CancellationToken>());
    }
}
