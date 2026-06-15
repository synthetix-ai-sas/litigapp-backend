# LitigApp — Backend

API .NET para LitigApp: SaaS que monitorea procesos judiciales en Colombia vía la API de la Rama Judicial y notifica a abogados por email cuando hay novedades.

## ⚠️ LO PRIMERO QUE DEBES HACER

Antes de escribir o diseñar cualquier cosa, lee estos archivos en orden:

1. `docs/blueprint.md` — **fuente de verdad completa** del sistema. Arquitectura, esquema de BD, endpoints, jobs, build order paso a paso. NO empieces a codificar sin haberlo leído completo.
2. `docs/api-rama-judicial.md` — documentación de los endpoints reales de la API Rama Judicial con ejemplos de respuesta. Crítico para los DTOs y el sync engine.

Si algo que te pido contradice el blueprint, **detente y avísame antes de continuar**.

Este repo es **solo el backend**. El frontend (Angular) vive en un repo separado (`litigapp-web`). Ambos comparten el mismo `blueprint.md` como contrato. Cuando construyas endpoints, respeta exactamente los contratos de la sección 5 del blueprint porque el frontend depende de ellos.

## Por dónde empezar

Sigue el **Build Order de la sección 11 del blueprint**, pasos backend (Steps 0-13):
- Step 0: Spike de validación de la API Rama Judicial (1 día).
- Step 1: Scaffolding de la solución (.NET 10, 5 csproj).
- ... hasta Step 13 (PDF con QuestPDF).

Trabaja **incrementalmente**: completa y verifica un step antes de pasar al siguiente. No avances sin que el step anterior compile y sus tests pasen.

## Commands

- `dotnet build` — build solución completa
- `dotnet run --project src/LitigApp.Api` — API en localhost:5000
- `dotnet test` — todos los tests
- `dotnet ef migrations add <Name> -p src/LitigApp.Infrastructure -s src/LitigApp.Api` — nueva migración
- `dotnet ef database update -p src/LitigApp.Infrastructure -s src/LitigApp.Api` — aplicar migraciones
- `dotnet run --project src/LitigApp.Api -- seed-catalog` — seed departamentos/municipios
- `docker compose up -d` — Postgres local (opcional para dev)

## Tech Stack

ASP.NET Core 10 (.NET 10 LTS, C# 14) + EF Core 10 + PostgreSQL 16 (Supabase) + Hangfire + ASP.NET Identity + JWT + Polly v8. Deploy en Railway (2 servicios: API + Worker).

## Arquitectura — Clean Architecture (5 csproj)

- `LitigApp.Domain` — entidades, value objects, eventos. Sin dependencias externas.
- `LitigApp.Application` — handlers CQRS (sin MediatR — handlers propios), validators, contratos (interfaces).
- `LitigApp.Infrastructure` — EF Core, Identity, `IRamaJudicialClient`, Resend, QuestPDF, ClosedXML, jobs services.
- `LitigApp.Jobs` — Hangfire jobs: OverviewSweep, ActionsSweep, BulkImport, dispatchers, cleanup.
- `LitigApp.Api` — controllers por feature, middleware, Program.cs.

**Regla de dependencias (inviolable)**: Domain ← Application ← Infrastructure ← Jobs ← Api. Nunca al revés.

### Data Flow
Request HTTP → Controller (Api) → Handler (Application) → Repository (Infrastructure) → EF Core → Postgres.
Job (Hangfire) → Service (Application/Infrastructure) → `IRamaJudicialClient` → API externa.

## Lo más crítico de este backend

1. **WAF de la Rama Judicial**: la API bloquea por IP tras ~186 requests en ráfaga (403, ~20min). TODO acceso debe ser trickle (2-3s + jitter) + rotación de User-Agent + detección de 403 → cooldown. Ver secciones 6 y 10 del blueprint.
2. **Solo 2 endpoints en sync diario**: overview + actions. detail/subjects solo en creación inicial.
3. **Notificaciones agregadas por usuario**, jamás por proceso. 1 email con N procesos, no N emails.
4. **Creación individual de proceso = síncrona** (sin Hangfire). Bulk import sí usa Hangfire.

## Code Organization Rules

1. **Clean Architecture inviolable**: nunca importar Infrastructure desde Application/Domain.
2. **Un handler por carpeta**: `Commands/CreateXxx/CreateXxxCommand.cs + Handler.cs + Validator.cs`.
3. **Result<T> en todos los handlers**: nunca throw para errores esperables.
4. **AsNoTracking en todas las queries de lectura**.
5. **async + CancellationToken** en todo I/O.
6. **Records para DTOs** y value objects inmutables.
7. **Migraciones EF versionadas en git**: nunca borres una aplicada.
8. **⚠️ REGLA CRÍTICA — NUNCA HAGAS COMMIT DIRECTO EN MAIN ⚠️**
   - SIEMPRE crea una rama `feature/{descripcion}` antes de codear cualquier cosa.
   - Antes de cualquier `git commit`, verifica con `git branch` que NO estás en `main`.
   - El único camino de código a `main` es vía PR con squash merge.
   - Si te das cuenta de que estás en `main` con cambios, haz `git checkout -b feature/xxx` ANTES de commitear.

## Herramientas disponibles en este repo

Tienes acceso a las siguientes herramientas además de las skills globales
(superpowers, gstack). ÚSALAS PROACTIVAMENTE:

### Roslyn Navigator (Semantic Code Navigation)
Utiliza las herramientas MCP de Roslyn Navigator en lugar de leer los archivos fuente cuando necesites comprender el código. Esto ahorra una cantidad significativa de tokens.

**Da preferencia a estas herramientas frente a `Read` cuando:**
- **Comiences una tarea sin contexto:** `get_project_graph` → comprender la estructura de la solución
- **Localices un tipo o método:** `find_symbol` → saltar directamente a la definición
- **Comprender la forma de un tipo:** `get_public_api`, `get_symbol_detail`
- **Rastrees cadenas de llamadas o realices análisis de impacto:** `find_callers`, `find_references`
- **Trabajes con interfaces o DI:** `find_implementations`, `get_type_hierarchy`
- **Trabajar con métodos virtuales/abstractos:** `find_overrides` para localizar todas las implementaciones concretas
- **Migrar de una interfáz ITernface1 a ITernface2:** `find_callers` para encontrar todos los consumidores antes de realizar el cambio
- **Añadir a la infraestructura:** `find_symbol` + `get_dependency_graph` para mapear el flujo existente
- **Antes de compilar o comprobar el estado del compilador:** `get_diagnostics` para ver advertencias y errores
- **Comprobación de la cobertura de pruebas:** `get_test_coverage_map` para identificar áreas sin probar
- **Comprobaciones de estado / deuda técnica:** `find_dead_code`, `detect_antipatterns`, `detect_circular_dependencies`

**Regla:** Prueba siempre primero una herramienta de Roslyn Navigator. Recurre a `Read` solo si la herramienta no devuelve suficientes detalles (por ejemplo, si necesitas el cuerpo completo del método).

### Skills de dotnet-toolkit a usar SIEMPRE

Estas skills definen patterns que TODOS los devs deben seguir en este
proyecto. No son opcionales:

- `dotnet-toolkit:minimal-api` — TODOS los endpoints son Minimal APIs con
  MapGroup. NO se usan controllers MVC. Patrón: clase estática
  `XxxEndpoints` con método `MapXxxEndpoints(IEndpointRouteBuilder)`
  registrada en Program.cs.

- `dotnet-toolkit:openapi` — OpenAPI nativo de .NET 10 (`AddOpenApi` +
  `MapOpenApi`). NO Swashbuckle. Documentar con `.WithName()`,
  `.WithSummary()`, `.WithTags()`, `.Produces<T>()`.

- `dotnet-toolkit:error-handling` — Result<T> pattern en handlers
  + ProblemDetails (RFC 9457) en endpoints. `TypedResults.Problem(...)`
  o handler global de excepciones. NO throw para errores esperables.

- `dotnet-toolkit:serilog` + `dotnet-toolkit:logging` — Serilog config
  + Request Logging middleware en Program.cs + LoggingBehavior CQRS
  para Commands. Queries simples NO requieren log por hit.

- `dotnet-toolkit:ef-core` — patterns de EF Core 10. AsNoTracking en
  reads, value converters para VOs cuando se introduzcan, compiled
  queries para hot paths del sync engine.

- `dotnet-toolkit:resilience` + `dotnet-toolkit:httpclient-factory` —
  TODOS los HttpClients (Rama Judicial, Resend, futuros) van por
  IHttpClientFactory con Microsoft.Extensions.Http.Resilience.
  Polly v8 underneath.

- `dotnet-toolkit:testing` — xUnit v3 + WebApplicationFactory +
  Testcontainers Postgres real para integration tests. Verify para
  snapshot testing de payloads complejos (digest emails, PDF, etc.).

- `dotnet-toolkit:configuration` — Options pattern con IOptionsSnapshot
  para todo lo configurable (Jwt, RamaJudicial, Sweep, Throttle, etc.).
  Validación de Options con `.ValidateDataAnnotations().ValidateOnStart()`.

- `dotnet-toolkit:modern-csharp` — C# 14 features cuando aporten:
  primary constructors, collection expressions, pattern matching
  exhaustivo. Records para DTOs y VOs.

- `dotnet-toolkit:clean-architecture` — referencia del patrón. El
  blueprint manda en discrepancias específicas de LitigApp.

### Skills de dotnet-toolkit para usar a demanda

- `dotnet-toolkit:scaffold` o `dotnet-toolkit:scaffolding` — al crear
  un feature slice nuevo.
- `dotnet-toolkit:tdd` — para implementar lógica crítica (sync engine,
  parsers, handlers no triviales).
- `dotnet-toolkit:plan` — antes de codear features de 3+ steps.
- `dotnet-toolkit:verify` — antes de abrir PR (7 fases: build,
  analyzers, antipatterns, tests, security, format, diff).
- `dotnet-toolkit:de-sloppify` — pasada de limpieza antes de PR.
- `dotnet-toolkit:code-review` — review propio antes de pedir review
  humana.
- `dotnet-toolkit:migrate` o `dotnet-toolkit:migration-workflow` — al
  agregar migraciones EF.
- `dotnet-toolkit:build-fix` — si el build se rompe en cascada.
- `dotnet-toolkit:health-check` — periódicamente para auditar calidad.
- `dotnet-toolkit:security-scan` — antes de cada deploy a producción.

### Skills de gstack relevantes para este proyecto
- `/autoplan` — antes de codear features complejas (sync engine, imports).
- `/plan-eng-review` — validar el plan antes de implementar.
- `/review` — code review propio antes de pedir review humana.
- `/ship` — para crear commit + push + PR con formato consistente.

### Skills de superpowers relevantes
- `superpowers:test-driven-development` — antes de implementar features
  no triviales (handlers, sync engine, parsers).
- `superpowers:systematic-debugging` — cuando encuentres bugs que no
  cedan en 15 minutos.
- `superpowers:verification-before-completion` — antes de declarar una
  tarea terminada (corre los comandos, no asumas).
- `superpowers:requesting-code-review` — antes de pedir PR review.

## Prioridad entre fuentes de verdad

Si hay conflicto entre las fuentes:
1. **Blueprint** (docs/blueprint.md) — máxima autoridad sobre decisiones de
   arquitectura, schema, contratos.
2. **Execution plan** (docs/execution-plan.md) — qué owner toca qué tarea
   y en qué orden.
3. **dotnet toolkit / convenciones .NET genéricas** — solo cuando el
   blueprint no se pronuncia.
4. **Sugerencias propias del modelo** — última prioridad.

## Environment Variables

Ver sección 12 del blueprint. Sensibles SOLO en env vars / user-secrets, nunca en código. `appsettings.json` versionado sin secretos; `appsettings.Development.json` en `.gitignore`.

## Reglas No Negociables

Ver sección 19 del blueprint (lista completa, 35 reglas). Las más importantes para backend:

1. WAF awareness: 403 → cooldown → resume. Trickle + jitter + UA rotation siempre.
2. Solo 2 endpoints diarios (overview + actions).
3. Notificaciones agregadas por usuario, triggered (no polling cada 5 min).
4. WhatsApp FUERA del MVP — solo stub `NoOpWhatsAppSender`.
5. BrightData NO activo en MVP (`Proxy.Enabled=false`).
6. Creación individual síncrona; bloqueo mutuo con import activo (409).
7. Excel: 2MB / 5000 filas máx.
8. Hangfire retention 24h.
9. Tests de integración con Testcontainers Postgres real.
10. TreatWarningsAsErrors. CQRS sin MediatR.
