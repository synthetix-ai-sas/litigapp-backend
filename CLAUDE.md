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