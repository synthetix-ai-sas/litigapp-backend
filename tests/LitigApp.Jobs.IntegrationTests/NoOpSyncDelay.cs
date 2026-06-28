using LitigApp.Application.Common.Abstractions;

namespace LitigApp.Jobs.IntegrationTests;

/// <summary>Test double — never actually sleeps, so integration tests stay fast.</summary>
public sealed class NoOpSyncDelay : ISyncDelay
{
    public Task WaitAsync(TimeSpan delay, CancellationToken ct) => Task.CompletedTask;
}
