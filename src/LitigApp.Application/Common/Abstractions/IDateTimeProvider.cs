namespace LitigApp.Application.Common.Abstractions;

/// <summary>
/// Abstraction over DateTime.UtcNow to allow deterministic testing.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>Returns the current UTC date/time.</summary>
    DateTime UtcNow { get; }
}
