using LitigApp.Application.Common.Abstractions;

namespace LitigApp.Api.IntegrationTests.Common;

/// <summary>
/// Deterministic fake of the Rama Judicial API for integration tests — never hits the
/// real WAF-protected endpoint. Behavior is driven by the file number:
///   • starts with "00000"  → overview returns null (process not found → 422)
///   • contains "88888"      → private process (esPrivado=true); the other 3 endpoints 404
///   • contains "77777"      → detail call fails (→ created with sync_status = "partial")
///   • otherwise             → full successful payload (→ sync_status = "ok")
/// </summary>
public sealed class FakeRamaJudicialClient : IRamaJudicialClient
{
    private const long PartialExternalId = 999;
    private const long PrivateExternalId = 888;
    private const long OkExternalId = 123456;

    public Task<RamaResult<OverviewData?>> GetOverviewByFileNumberAsync(string fileNumber, CancellationToken ct)
    {
        if (fileNumber.StartsWith("00000", StringComparison.Ordinal))
            return Task.FromResult(RamaResult<OverviewData?>.Ok(null));

        // Private process: esPrivado=true, no last-action date. Mirrors the real API's payload.
        if (fileNumber.Contains("88888", StringComparison.Ordinal))
            return Task.FromResult(RamaResult<OverviewData?>.Ok(new OverviewData(
                PrivateExternalId, 7, fileNumber, LastActionDate: null,
                "JUZGADO 002 CIVIL MUNICIPAL DE MANIZALES", "Caldas", IsPrivate: true)));

        var externalId = fileNumber.Contains("77777", StringComparison.Ordinal) ? PartialExternalId : OkExternalId;
        var overview = new OverviewData(
            externalId, 7, fileNumber, new DateTime(2026, 3, 20),
            "JUZGADO 002 CIVIL MUNICIPAL DE MANIZALES", "Caldas", false);
        return Task.FromResult(RamaResult<OverviewData?>.Ok(overview));
    }

    public Task<RamaResult<ProcessDetailData?>> GetDetailAsync(long externalProcessId, CancellationToken ct)
    {
        // Private processes 404 on detail — a terminal, known result. If creation ever calls
        // this for a private process, the outcome would flip to "partial", failing the test.
        if (externalProcessId == PrivateExternalId)
            return Task.FromResult(RamaResult<ProcessDetailData?>.Fail(FailureKind.Private, "No se puede ver el detalle de un proceso privado"));

        if (externalProcessId == PartialExternalId)
            return Task.FromResult(RamaResult<ProcessDetailData?>.Fail(FailureKind.Transient, "detalle no disponible"));

        var detail = new ProcessDetailData(
            "170014003010", "JUZGADO 002 CIVIL MUNICIPAL DE MANIZALES", 7, false, new DateTime(2024, 1, 1),
            "De Ejecución", "Ejecutivo Singular", "Por sumas de dinero", "Sin Tipo de Recurso", "Juez X", "contenido");
        return Task.FromResult(RamaResult<ProcessDetailData?>.Ok(detail));
    }

    public Task<RamaResult<List<SubjectData>>> GetSubjectsAsync(long externalProcessId, CancellationToken ct)
    {
        if (externalProcessId == PrivateExternalId)
            return Task.FromResult(RamaResult<List<SubjectData>>.Fail(FailureKind.Private, "No se puede ver el detalle de un proceso privado"));

        return Task.FromResult(RamaResult<List<SubjectData>>.Ok(new List<SubjectData>
        {
            new(1, "Demandante", false, "123", "OSCAR ARTURO ORTIZ HENAO"),
            new(2, "Demandado", false, "456", "FRANCISCA HELENA GONZALEZ ARIAS"),
        }));
    }

    public Task<RamaResult<List<ActionData>>> GetFirstPageActionsAsync(long externalProcessId, CancellationToken ct)
    {
        if (externalProcessId == PrivateExternalId)
            return Task.FromResult(RamaResult<List<ActionData>>.Fail(FailureKind.Private, "No se puede ver el detalle de un proceso privado"));

        return Task.FromResult(RamaResult<List<ActionData>>.Ok(new List<ActionData>
        {
            new(1001, 82, new DateTime(2026, 3, 20), "Fijacion estado", "nota", null, null, new DateTime(2026, 3, 20), null, false),
        }));
    }
}
