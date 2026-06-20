namespace LitigApp.Application.Features.Processes;

/// <summary>
/// Stable error codes returned in <c>Result.Error</c> by process handlers.
/// The API layer maps each code to an HTTP status + ProblemDetails.
/// </summary>
public static class ProcessErrorCodes
{
    public const string InvalidFileNumber = "INVALID_FILE_NUMBER";
    public const string InvalidConsecutive = "INVALID_CONSECUTIVE";
    public const string CourtNotFound = "COURT_NOT_FOUND";
    public const string ImportInProgress = "IMPORT_IN_PROGRESS";
    public const string DuplicateProcess = "DUPLICATE_PROCESS";
    public const string ProcessNotFoundInRama = "PROCESS_NOT_FOUND_IN_RAMA";
    public const string RamaOverviewFailed = "RAMA_OVERVIEW_FAILED";
    public const string ProcessNotFound = "PROCESS_NOT_FOUND";
}
