namespace LitigApp.Application.Common.Abstractions;

/// <summary>
/// Discriminated union: either a successful value T or a RamaJudicialFailure.
/// Keeps FailureKind typed and avoids exceptions for expected errors.
/// </summary>
public sealed class RamaResult<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public RamaJudicialFailure? Failure { get; }

    private RamaResult(bool isSuccess, T? value, RamaJudicialFailure? failure)
    {
        IsSuccess = isSuccess;
        Value = value;
        Failure = failure;
    }

    public static RamaResult<T> Ok(T value) => new(true, value, null);
    public static RamaResult<T> Fail(RamaJudicialFailure failure) => new(false, default, failure);

    /// <summary>Convenience factory via FailureKind + message string.</summary>
    public static RamaResult<T> Fail(FailureKind kind, string message) =>
        Fail(new RamaJudicialFailure(kind, message));
}
