using LitigApp.Application.Features.Processes;

namespace LitigApp.Api.Features.Processes;

/// <summary>
/// Maps a process error code (carried in <c>Result.Error</c>) to an HTTP ProblemDetails.
/// The stable code is exposed in <c>extensions["code"]</c> for the frontend.
/// </summary>
internal static class ProcessProblem
{
    public static IResult From(string? code)
    {
        var (status, title) = code switch
        {
            ProcessErrorCodes.InvalidFileNumber => (StatusCodes.Status400BadRequest, "Radicado inválido."),
            ProcessErrorCodes.InvalidConsecutive => (StatusCodes.Status400BadRequest, "Consecutivo inválido."),
            ProcessErrorCodes.CourtNotFound => (StatusCodes.Status400BadRequest, "Despacho inválido."),
            ProcessErrorCodes.ImportInProgress => (StatusCodes.Status409Conflict, "Hay una importación en curso."),
            ProcessErrorCodes.DuplicateProcess => (StatusCodes.Status409Conflict, "El proceso ya existe."),
            ProcessErrorCodes.ProcessNotFoundInRama => (StatusCodes.Status422UnprocessableEntity, "Proceso no encontrado en Rama Judicial."),
            ProcessErrorCodes.RamaOverviewFailed => (StatusCodes.Status503ServiceUnavailable, "No se pudo consultar la Rama Judicial. Intenta de nuevo."),
            ProcessErrorCodes.ProcessNotFound => (StatusCodes.Status404NotFound, "Proceso no encontrado."),
            ProcessErrorCodes.ProcessDataIncomplete => (StatusCodes.Status409Conflict, "Los datos del proceso aún están incompletos."),
            _ => (StatusCodes.Status500InternalServerError, "Error inesperado."),
        };

        return TypedResults.Problem(
            statusCode: status,
            title: title,
            extensions: new Dictionary<string, object?> { ["code"] = code });
    }
}
