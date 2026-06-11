# API Spike — Rama Judicial: Hallazgos

> Ejecutado: 2026-06-07  
> Radicado de prueba: `17001400301020240019200`  
> Rama: `feature/rama-judicial-client`

---

## 1. Endpoints confirmados

| Endpoint | URL | Estado |
|---|---|---|
| Overview | `GET /api/v2/Procesos/Consulta/NumeroRadicacion?numero={23digits}&SoloActivos=false&pagina=1` | ✅ OK |
| Detalle | `GET /api/v2/Proceso/Detalle/{idProceso}` | ✅ OK |
| Sujetos | `GET /api/v2/Proceso/Sujetos/{idProceso}?pagina=1` | ✅ OK |
| Actuaciones | `GET /api/v2/Proceso/Actuaciones/{idProceso}?pagina=1` | ✅ OK |
| Documentos | `GET /api/v2/Proceso/Documentos/{idProceso}` | ❌ Nunca retornó respuesta exitosa — **no usar en MVP** |

---

## 2. Tiempos de respuesta observados

| Endpoint | Tiempo típico | Notas |
|---|---|---|
| Overview (API estable) | ~4.5 s | Lento pero consistente |
| Overview (DB con problemas) | 10–35 s → 404/timeout | Ver quirk #1 |
| Detalle | ~100 ms | Muy rápido |
| Sujetos | ~100 ms | Muy rápido |
| Actuaciones | ~230 ms | Rápido |

**Implicación**: el `TimeoutSeconds` debe ser ≥ 30 s para el overview. El valor inicial de 15 s en el blueprint es insuficiente cuando la API tiene problemas de BD. Se ajusta a `30` en `appsettings.json`.

---

## 3. Hallazgo crítico: `idProceso` vs `idRegProceso`

El blueprint anticipó discrepancia — confirmada:

| Campo | Endpoint origen | Valor en ejemplo | Dígitos |
|---|---|---|---|
| `idProceso` (overview) | `GET /Procesos/Consulta/...` | `132573703` | **9 dígitos** |
| `idRegProceso` (detalle) | `GET /Proceso/Detalle/...` | `13257370` | **8 dígitos** |

**Son IDs distintos.** El endpoint `/Detalle/{id}` recibe el `idProceso` del overview (9 dígitos) como path param. En la respuesta retorna `idRegProceso` (diferente número, 8 dígitos). No es un typo — son referencias internas del sistema de la Rama Judicial.

**Consecuencia para el código**: `external_process_id` en la tabla `processes` almacena el `idProceso` del overview (el usado para llamar detail/sujetos/actuaciones). `idRegProceso` no es necesario persistir.

---

## 4. DTOs — validación de deserialización

Todos los DTOs del blueprint §6.2 deserializan correctamente con `PropertyNamingPolicy = CamelCase` + `PropertyNameCaseInsensitive = true`.

### Campos extra en la API (no en blueprint):

| Entidad | Campo extra | Tipo | Valor observado | Acción |
|---|---|---|---|---|
| `OverviewProcess` | `cantFilas` | `int` | `-1` | Ignorar (no útil) |
| `SubjectDto` | `cant` | `int` | Total sujetos | Ignorar |
| `ActionDto` | `cant` | `int` | Total actuaciones | Ignorar |

### Quirk: `codRegla` con espacios al final

El campo `codRegla` en actuaciones viene con padding de espacios:
```
"00                              "
```
**Acción**: hacer `.Trim()` en el mapper antes de persistir.

### Tipos confirmados:

| Campo | Tipo .NET correcto | Notas |
|---|---|---|
| `idProceso` | `long` | 132573703 — confirma 9 dígitos, cabe en long |
| `idRegActuacion` | `long` | 2308991013 — supera int.MaxValue, requiere long |
| `idRegSujeto` | `long` | OK como long |
| `fechaActuacion` | `DateTime?` | Nullable, no siempre presente |
| `fechaInicial`, `fechaFinal` | `DateTime?` | Nullable — muchas actuaciones sin términos |

---

## 5. Quirks de errores — mapeo a `FailureKind`

| Situación | HTTP | Body | `FailureKind` resultante |
|---|---|---|---|
| Radicado con 22 dígitos | 404 | `"El parametro \"NumeroRadicacion\" ha de contener 23 digitos."` | `InvalidInput` |
| Detalle: ID inexistente | 404 | `"No se encontro detalle para el IdProceso: X"` | `NotFound` |
| Actuaciones: proceso sin actuaciones | 404 | `"No se encontraron Actuaciones para el Proceso: X"` | → retornar lista vacía |
| Overview: radicado inexistente con DB lenta | 404 | `"Connection Timeout Expired..."` | `Transient` (reintentar) |
| Sujetos/Actuaciones: error interno | 500 | `"Object reference not set..."` | `Transient` |
| WAF | 403 | HTML o vacío | `WafBlocked` |
| Timeout HttpClient | — | `TaskCanceledException` | `Transient` |

**Hallazgo crítico**: La API retorna `404` tanto para "proceso no encontrado" como para "error de BD interno". El cliente DEBE parsear el `Message` del body para distinguirlos. Un `404` con texto `"Connection Timeout"` es transiente (reintentar); con `"No se encontro"` es definitive (NotFound).

---

## 6. Comportamiento WAF

- No se observaron 403s en el spike (7 requests con ~2s de delay entre ellos).
- La documentación del blueprint (§6.0) confirma bloqueo tras ~186 requests en ráfaga.
- **Validado**: el trickle de 2–3 s previene el WAF efectivamente en nuestro volumen.
- La estrategia del blueprint permanece sin cambios.

---

## 7. Paginación de actuaciones

- Ejemplo real: 82 actuaciones totales, 40 por página, 3 páginas.
- Para MVP consumimos **solo página 1** (40 más recientes) — decisión confirmada.
- La página 1 incluye las más recientes ordenadas por `consActuacion` DESC.

---

## 8. Ajustes al blueprint §6.2

Los DTOs del blueprint son correctos. Solo se añaden los campos extra ignorados:

```csharp
// En Infrastructure DTOs (campos extra de la API, no en el contrato Application):
public int CantFilas { get; init; }  // OverviewProcess — siempre -1, ignorar
public int Cant { get; init; }        // SubjectDto, ActionDto — total registros, ignorar
```

El campo `codRegla` necesita `.Trim()` antes de persistir.

---

## 9. Configuración ajustada

```json
"RamaJudicial": {
  "TimeoutSeconds": 30  // ← ajustado de 15 a 30 por latencia real del overview
}
```

El resto de parámetros del blueprint §6.1 se mantienen.
