using LitigApp.Application.Common.Abstractions;

namespace LitigApp.Infrastructure.Time;

/// <summary>
/// Production implementation of IDateTimeProvider that delegates to DateTime.UtcNow.
/// Tests inject a fake implementation for deterministic time control.
/// </summary>
internal sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
