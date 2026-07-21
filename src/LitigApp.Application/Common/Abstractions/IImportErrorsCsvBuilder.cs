using LitigApp.Application.Features.Notifications.Dtos;

namespace LitigApp.Application.Common.Abstractions;

/// <summary>
/// Builds the "procesos_con_errores.csv" bytes shared by the ImportComplete email attachment
/// and the GET /imports/{id}/errors.csv download endpoint — same builder, so the CSV is
/// byte-for-byte identical in both (blueprint §9 "CSV de errores").
/// </summary>
public interface IImportErrorsCsvBuilder
{
    byte[] Build(IReadOnlyList<ImportErrorRow> errors);
}
