namespace LitigApp.Application.Features.Imports;

/// <summary>Stable error codes for import endpoints (surfaced in ProblemDetails extensions["code"]).</summary>
public static class ImportErrorCodes
{
    public const string FileTooLarge = "FILE_TOO_LARGE";
    public const string EmptyFile = "EMPTY_FILE";
    public const string InvalidFile = "INVALID_FILE";
    public const string TooManyRows = "TOO_MANY_ROWS";
}
