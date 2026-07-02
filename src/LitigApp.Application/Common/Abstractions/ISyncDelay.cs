namespace LitigApp.Application.Common.Abstractions;

/// <summary>
/// Injectable delay so the sweep jobs can pace themselves between API calls (adaptive
/// throttle lives in the job, not the HTTP client — decision D1) while staying testable
/// (unit tests pass a no-op instead of actually sleeping).
/// </summary>
public interface ISyncDelay
{
    Task WaitAsync(TimeSpan delay, CancellationToken ct);
}
