using LitigApp.Application.Features.Imports;

namespace LitigApp.Api.Features.Imports;

/// <summary>Maps an import error code to a ProblemDetails; the code is exposed in extensions["code"].</summary>
internal static class ImportProblem
{
    public static IResult From(string code)
    {
        var (status, title) = code switch
        {
            ImportErrorCodes.FileTooLarge => (StatusCodes.Status413PayloadTooLarge, "El archivo supera el tamaño máximo permitido."),
            ImportErrorCodes.EmptyFile => (StatusCodes.Status422UnprocessableEntity, "El archivo está vacío."),
            ImportErrorCodes.InvalidFile => (StatusCodes.Status422UnprocessableEntity, "El archivo no es un .xlsx válido."),
            ImportErrorCodes.TooManyRows    => (StatusCodes.Status422UnprocessableEntity, "El archivo excede el máximo de filas permitidas."),
            ImportErrorCodes.PreviewExpired => (StatusCodes.Status404NotFound, "El preview ha expirado. Sube el archivo de nuevo."),
            ImportErrorCodes.ImportInProgress => (StatusCodes.Status409Conflict, "Ya tienes una importación en curso."),
            _ => (StatusCodes.Status500InternalServerError, "Error inesperado."),
        };

        return TypedResults.Problem(
            statusCode: status,
            title: title,
            extensions: new Dictionary<string, object?> { ["code"] = code });
    }
}
