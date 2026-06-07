namespace LitigApp.Infrastructure.ExternalApis.RamaJudicial.Dtos;

/// <summary>
/// Represents the error body returned by the Rama Judicial API on failure responses.
/// Used to extract the Message field for FailureKind discrimination.
/// </summary>
internal sealed class ApiErrorResponse
{
    public string? Message { get; init; }
    public string? ExceptionMessage { get; init; }
}
