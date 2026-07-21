namespace LitigApp.Application.Features.Notifications.Dtos;

/// <summary>
/// One row of <c>import_jobs.errors</c> (<c>[{ row, radicado, code, message }]</c>), shaped
/// for the shared CSV builder. Deliberately separate from Jobs' own row-error type — Application
/// cannot depend on the Jobs project (Clean Architecture: Domain ← Application ← Infrastructure
/// ← Jobs ← Api). BulkImportJob maps into this shape when building the outbox payload.
/// </summary>
public sealed record ImportErrorRow(int Row, string Radicado, string Code, string Message);
