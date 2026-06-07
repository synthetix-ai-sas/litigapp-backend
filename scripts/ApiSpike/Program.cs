/// <summary>
/// API Spike — Rama Judicial
/// Tarea 1.C del execution plan: validar endpoints, DTOs y comportamiento WAF.
/// NO agregar a LitigApp.sln. Ejecutar con: dotnet run --project scripts/ApiSpike
/// </summary>

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

const string BaseUrl = "https://consultaprocesos.ramajudicial.gov.co:448";
const string ValidRadicado = "17001400301020240019200";
const string InvalidRadicado = "1700140030102024001920"; // 22 digits — missing last digit

var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = true
};

using var http = new HttpClient
{
    BaseAddress = new Uri(BaseUrl),
    Timeout = TimeSpan.FromSeconds(35)
};

http.DefaultRequestHeaders.TryAddWithoutValidation(
    "User-Agent",
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36");
http.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json, text/plain, */*");
http.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "es-ES,es;q=0.9");
http.DefaultRequestHeaders.TryAddWithoutValidation("Origin", "https://consultaprocesos.ramajudicial.gov.co");
http.DefaultRequestHeaders.TryAddWithoutValidation("Referer", "https://consultaprocesos.ramajudicial.gov.co/");

Console.WriteLine("=".PadRight(70, '='));
Console.WriteLine("LITIGAPP — API SPIKE contra Rama Judicial");
Console.WriteLine("=".PadRight(70, '='));

// ── TEST 1: Overview radicado VÁLIDO ──────────────────────────────────────────
Console.WriteLine("\n[1] Overview — radicado VÁLIDO: " + ValidRadicado);
var sw = Stopwatch.StartNew();
var r1 = await http.GetAsync(
    $"/api/v2/Procesos/Consulta/NumeroRadicacion?numero={ValidRadicado}&SoloActivos=false&pagina=1");
sw.Stop();
Console.WriteLine($"    Status: {(int)r1.StatusCode} {r1.StatusCode} — {sw.ElapsedMilliseconds}ms");

OverviewResponse? overview = null;
if (r1.IsSuccessStatusCode)
{
    var body = await r1.Content.ReadAsStringAsync();
    overview = JsonSerializer.Deserialize<OverviewResponse>(body, jsonOptions);
    Console.WriteLine($"    tipoConsulta: {overview?.TipoConsulta}");
    Console.WriteLine($"    procesos.Count: {overview?.Procesos?.Count}");
    if (overview?.Procesos?.Count > 0)
    {
        var p = overview.Procesos[0];
        Console.WriteLine($"    idProceso: {p.IdProceso}  (type={p.IdProceso.GetType().Name}, digits={p.IdProceso.ToString().Length})");
        Console.WriteLine($"    idConexion: {p.IdConexion}");
        Console.WriteLine($"    llaveProceso: {p.LlaveProceso}");
        Console.WriteLine($"    despacho: {p.Despacho?.Trim()}");
        Console.WriteLine($"    fechaUltimaActuacion: {p.FechaUltimaActuacion}");
        Console.WriteLine($"    cantFilas: {p.CantFilas}");
    }
    Console.WriteLine($"    paginacion.cantidadRegistros: {overview?.Paginacion?.CantidadRegistros}");
}
else
{
    Console.WriteLine("    BODY: " + await r1.Content.ReadAsStringAsync());
}

// ── TEST 2: Overview radicado INVÁLIDO ───────────────────────────────────────
await Task.Delay(2000);
Console.WriteLine("\n[2] Overview — radicado INVÁLIDO (22 dígitos): " + InvalidRadicado);
sw.Restart();
var r2 = await http.GetAsync(
    $"/api/v2/Procesos/Consulta/NumeroRadicacion?numero={InvalidRadicado}&SoloActivos=false&pagina=1");
sw.Stop();
Console.WriteLine($"    Status: {(int)r2.StatusCode} {r2.StatusCode} — {sw.ElapsedMilliseconds}ms");
Console.WriteLine("    BODY: " + await r2.Content.ReadAsStringAsync());

// ── TEST 3: Detalle ───────────────────────────────────────────────────────────
var idProceso = overview?.Procesos?.FirstOrDefault()?.IdProceso;
if (idProceso.HasValue)
{
    await Task.Delay(2000);
    Console.WriteLine($"\n[3] Detalle — idProceso: {idProceso}");
    sw.Restart();
    var r3 = await http.GetAsync($"/api/v2/Proceso/Detalle/{idProceso}");
    sw.Stop();
    Console.WriteLine($"    Status: {(int)r3.StatusCode} {r3.StatusCode} — {sw.ElapsedMilliseconds}ms");
    if (r3.IsSuccessStatusCode)
    {
        var body = await r3.Content.ReadAsStringAsync();
        var detail = JsonSerializer.Deserialize<ProcessDetailResponse>(body, jsonOptions);
        Console.WriteLine($"    idRegProceso: {detail?.IdRegProceso}  ← DISTINTO a idProceso={idProceso}? {detail?.IdRegProceso != idProceso}");
        Console.WriteLine($"    codDespachoCompleto: {detail?.CodDespachoCompleto}");
        Console.WriteLine($"    tipoProceso: {detail?.TipoProceso}");
        Console.WriteLine($"    claseProceso: {detail?.ClaseProceso}");
        Console.WriteLine($"    subclaseProceso: {detail?.SubclaseProceso}");
        Console.WriteLine($"    ponente: {detail?.Ponente}");
    }
    else
    {
        Console.WriteLine("    BODY: " + await r3.Content.ReadAsStringAsync());
    }

    // ── TEST 4: Sujetos ───────────────────────────────────────────────────────
    await Task.Delay(2000);
    Console.WriteLine($"\n[4] Sujetos — idProceso: {idProceso}");
    sw.Restart();
    var r4 = await http.GetAsync($"/api/v2/Proceso/Sujetos/{idProceso}?pagina=1");
    sw.Stop();
    Console.WriteLine($"    Status: {(int)r4.StatusCode} {r4.StatusCode} — {sw.ElapsedMilliseconds}ms");
    if (r4.IsSuccessStatusCode)
    {
        var body = await r4.Content.ReadAsStringAsync();
        var sujetos = JsonSerializer.Deserialize<SubjectsResponse>(body, jsonOptions);
        Console.WriteLine($"    sujetos.Count: {sujetos?.Sujetos?.Count}");
        Console.WriteLine($"    paginacion.cantidadRegistros: {sujetos?.Paginacion?.CantidadRegistros}");
        foreach (var s in sujetos?.Sujetos ?? [])
            Console.WriteLine($"    → [{s.TipoSujeto}] {s.NombreRazonSocial}  idRegSujeto={s.IdRegSujeto}");
    }
    else
    {
        Console.WriteLine("    BODY: " + await r4.Content.ReadAsStringAsync());
    }

    // ── TEST 5: Actuaciones ───────────────────────────────────────────────────
    await Task.Delay(2000);
    Console.WriteLine($"\n[5] Actuaciones — idProceso: {idProceso}, página 1");
    sw.Restart();
    var r5 = await http.GetAsync($"/api/v2/Proceso/Actuaciones/{idProceso}?pagina=1");
    sw.Stop();
    Console.WriteLine($"    Status: {(int)r5.StatusCode} {r5.StatusCode} — {sw.ElapsedMilliseconds}ms");
    if (r5.IsSuccessStatusCode)
    {
        var body = await r5.Content.ReadAsStringAsync();
        var actuaciones = JsonSerializer.Deserialize<ActionsResponse>(body, jsonOptions);
        Console.WriteLine($"    actuaciones en pág 1: {actuaciones?.Actuaciones?.Count}");
        Console.WriteLine($"    paginacion.cantidadPaginas: {actuaciones?.Paginacion?.CantidadPaginas}");
        Console.WriteLine($"    paginacion.cantidadRegistros: {actuaciones?.Paginacion?.CantidadRegistros}");
        if (actuaciones?.Actuaciones?.Count > 0)
        {
            var first = actuaciones.Actuaciones[0];
            Console.WriteLine($"    [0] consActuacion={first.ConsActuacion}  idRegActuacion={first.IdRegActuacion}  type={first.IdRegActuacion.GetType().Name}");
            Console.WriteLine($"    [0] actuacion='{first.Actuacion}'");
            Console.WriteLine($"    [0] codRegla raw='{first.CodRegla}'  trimmed='{first.CodRegla?.Trim()}'");
        }
    }
    else
    {
        Console.WriteLine("    BODY: " + await r5.Content.ReadAsStringAsync());
    }
}

// ── TEST 6: Detalle con idProceso INEXISTENTE ─────────────────────────────────
await Task.Delay(2000);
Console.WriteLine("\n[6] Detalle — idProceso INEXISTENTE (142573703)");
sw.Restart();
var r6 = await http.GetAsync("/api/v2/Proceso/Detalle/142573703");
sw.Stop();
Console.WriteLine($"    Status: {(int)r6.StatusCode} {r6.StatusCode} — {sw.ElapsedMilliseconds}ms");
Console.WriteLine("    BODY: " + await r6.Content.ReadAsStringAsync());

// ── TEST 7: Overview radicado válido en formato, inexistente en sistema ────────
await Task.Delay(2000);
Console.WriteLine("\n[7] Overview — radicado inexistente (00001400301020240019200)");
sw.Restart();
var r7 = await http.GetAsync(
    "/api/v2/Procesos/Consulta/NumeroRadicacion?numero=00001400301020240019200&SoloActivos=false&pagina=1");
sw.Stop();
Console.WriteLine($"    Status: {(int)r7.StatusCode} {r7.StatusCode} — {sw.ElapsedMilliseconds}ms");
Console.WriteLine("    BODY: " + await r7.Content.ReadAsStringAsync());

Console.WriteLine("\n" + "=".PadRight(70, '='));
Console.WriteLine("SPIKE COMPLETADO");
Console.WriteLine("=".PadRight(70, '='));

// ─── DTOs ────────────────────────────────────────────────────────────────────
public sealed class OverviewResponse
{
    public string TipoConsulta { get; init; } = "";
    public List<OverviewProcess> Procesos { get; init; } = new();
    public Pagination Paginacion { get; init; } = new();
}

public sealed class OverviewProcess
{
    public long IdProceso { get; init; }
    public int IdConexion { get; init; }
    public string LlaveProceso { get; init; } = "";
    public DateTime? FechaProceso { get; init; }
    public DateTime? FechaUltimaActuacion { get; init; }
    public string Despacho { get; init; } = "";
    public string Departamento { get; init; } = "";
    public string SujetosProcesales { get; init; } = "";
    public bool EsPrivado { get; init; }
    public int CantFilas { get; init; } // present in real API, not in blueprint DTO
}

public sealed class ProcessDetailResponse
{
    public long IdRegProceso { get; init; }
    public string LlaveProceso { get; init; } = "";
    public int IdConexion { get; init; }
    public bool EsPrivado { get; init; }
    public DateTime? FechaProceso { get; init; }
    public string CodDespachoCompleto { get; init; } = "";
    public string Despacho { get; init; } = "";
    public string? Ponente { get; init; }
    public string? TipoProceso { get; init; }
    public string? ClaseProceso { get; init; }
    public string? SubclaseProceso { get; init; }
    public string? Recurso { get; init; }
    public string? Ubicacion { get; init; }
    public string? ContenidoRadicacion { get; init; }
    public DateTime FechaConsulta { get; init; }
    public DateTime UltimaActualizacion { get; init; }
}

public sealed class SubjectsResponse
{
    public List<SubjectDto> Sujetos { get; init; } = new();
    public Pagination Paginacion { get; init; } = new();
}

public sealed class SubjectDto
{
    public long IdRegSujeto { get; init; }
    public string TipoSujeto { get; init; } = "";
    public bool EsEmplazado { get; init; }
    public string? Identificacion { get; init; }
    public string NombreRazonSocial { get; init; } = "";
    public int Cant { get; init; }
}

public sealed class ActionsResponse
{
    public List<ActionDto> Actuaciones { get; init; } = new();
    public Pagination Paginacion { get; init; } = new();
}

public sealed class ActionDto
{
    public long IdRegActuacion { get; init; }
    public string LlaveProceso { get; init; } = "";
    public int ConsActuacion { get; init; }
    public DateTime? FechaActuacion { get; init; }
    public string Actuacion { get; init; } = "";
    public string? Anotacion { get; init; }
    public DateTime? FechaInicial { get; init; }
    public DateTime? FechaFinal { get; init; }
    public DateTime? FechaRegistro { get; init; }
    public string? CodRegla { get; init; }
    public bool ConDocumentos { get; init; }
    public int Cant { get; init; }
}

public sealed class Pagination
{
    public int CantidadRegistros { get; init; }
    public int RegistrosPagina { get; init; }
    public int CantidadPaginas { get; init; }
    public int Pagina { get; init; }
}
