using LitigApp.Application.Common.Abstractions;

namespace LitigApp.Infrastructure.Time;

/// <summary>Production <see cref="ISyncDelay"/> — a plain <see cref="Task.Delay(TimeSpan, CancellationToken)"/>.</summary>
internal sealed class RealSyncDelay : ISyncDelay
{
    public Task WaitAsync(TimeSpan delay, CancellationToken ct) => Task.Delay(delay, ct);
}
