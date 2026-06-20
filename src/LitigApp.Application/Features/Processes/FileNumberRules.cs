using LitigApp.Domain.Common;

namespace LitigApp.Application.Features.Processes;

/// <summary>
/// Validation and composition of the 23-digit "radicado" (file number).
/// Layout: court official code (12) + filing year (4) + consecutive (7).
/// Per the Rama Judicial API docs, a short consecutive is padded with trailing
/// zeros to reach 7 digits (e.g. "192" → "1920000").
/// </summary>
public static class FileNumberRules
{
    public const int Length = 23;

    public static bool IsValid(string? fileNumber) =>
        !string.IsNullOrWhiteSpace(fileNumber)
        && fileNumber.Length == Length
        && fileNumber.All(char.IsDigit);

    /// <summary>Composes a 23-digit file number from wizard parts, or a coded failure.</summary>
    public static Result<string> Compose(string officialCode, int filingYear, string consecutive)
    {
        if (officialCode is null || officialCode.Length != 12 || !officialCode.All(char.IsDigit))
            return Result<string>.Failure(ProcessErrorCodes.CourtNotFound);

        if (filingYear < 1900 || filingYear > 2100)
            return Result<string>.Failure(ProcessErrorCodes.InvalidFileNumber);

        var consec = (consecutive ?? string.Empty).Trim();
        if (consec.Length is 0 or > 7 || !consec.All(char.IsDigit))
            return Result<string>.Failure(ProcessErrorCodes.InvalidConsecutive);

        var paddedConsec = consec.PadRight(7, '0');
        return Result<string>.Success($"{officialCode}{filingYear:D4}{paddedConsec}");
    }
}
