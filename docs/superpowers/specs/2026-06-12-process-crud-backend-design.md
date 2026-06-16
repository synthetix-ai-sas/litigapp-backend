# Diseño — Tarea 2.B: Process CRUD backend

**Fecha:** 2026-06-12 · **Owner:** Santiago Gutiérrez (Persona B) · **Tarea del execution plan:** 2.B

> Nota: este diseño se actualizó tras sincronizar con `main`. Cuando se escribió el
> borrador inicial, 1.A (Auth) no estaba mergeado y se planeó un seam `ICurrentUser`
> propio. Ahora **1.A (#7), Serilog/LoggingBehavior (#8) y Hangfire/2.C (#9) están
> mergeados**, así que 2.B está formalmente desbloqueado y se reutilizan las piezas
> existentes en vez de crearlas.

## 1. Contexto verificado contra `main`

- Esquema (`processes`, `process_actions`, `process_subjects`, `import_jobs`) **ya
  migrado** en `20260604140948_Initial` → **sin migración nueva**.
- Entidades de dominio **POCO anémicas** (setters públicos, `SyncStatus`/`SyncPhase`
  como `string`) → seguimos el código real, no el dominio rico del blueprint §4.2.
- **Ya existen y se reutilizan:**
  - `Result<T>` (`Domain/Common/Result.cs`) — basado en string de error.
  - `ICommandHandler<TCommand,TResponse>` / `IQuery`/`IQueryHandler` / `Unit`.
  - `ICurrentUserService` (UserId desde claim `sub`) + impl `CurrentUserService`.
  - `LoggingBehavior` decora todos los `ICommandHandler<,>` (Scrutor `Decorate`).
  - `IProcessRepository` + `ProcessRepository` (métodos del sync engine).
  - `IRamaJudicialClient` (1.C) con `RamaResult<T>` + `FailureKind`.
- `processes.user_id` es `text` (FK lógica) → sin acoplamiento de migración con Auth.

## 2. Decisiones

| Tema | Elección |
|---|---|
| PRs | 2: **PR1** lectura, **PR2** escritura |
| Envelope | Objeto pelado (`TypedResults`) + `ProblemDetails`, como catalog |
| Mapeo de errores | `Result<T>.Error` lleva un **código estable** (p.ej. `DUPLICATE_PROCESS`); el endpoint lo traduce a status + `ProblemDetails` (sin tocar el tipo `Result` compartido) |
| Fallback parcial | Persistir `sync_phase="pending_partial_completion"` + seam `IPartialFetchScheduler` (NoOp hoy; Hangfire ya disponible para enchufar luego) |
| Alcance | Completo (novelties, list, detail, full-number, wizard, mark-attended, soft-delete) |
| Dashboard shell (Angular) | Fuera de alcance (repo `litigapp-web`) |

## 3. PR1 — Lectura (`feature/process-read-backend`)

Capa Application (todo `AsNoTracking`, filtrado por `ICurrentUserService.UserId`):

- `Common/Models/PagedResult<T>` → `{ Items, Total, Page, PageSize, TotalPages }`.
- `Common/Models/ProcessListFilter` → filtros opcionales de la lista.
- DTOs en `Features/Processes/Dtos/`: `ProcessListItemDto`, `ProcessDetailDto`
  (+ `CourtSummaryDto`, `ProcessSubjectDto`, `ProcessActionDto`).
- `Common/Abstractions/IProcessReader` (espejo de `ICatalogReader`) con:
  `ListNoveltiesAsync`, `ListAsync`, `GetByIdAsync` — impl EF en Infrastructure.
- Queries + handlers:
  - `ListNoveltiesQuery` → `GET /api/v1/processes/novelties?page&pageSize`
    (`attended=false`, activos, orden `last_court_action_at DESC`).
  - `ListProcessesQuery` → `GET /api/v1/processes` con filtros
    `courtName, fileNumber, subjectName, status, fromDate, toDate, attended`.
  - `GetProcessByIdQuery` → `GET /api/v1/processes/{id}` (detalle; `null` ⇒ 404;
    `canDownloadPdf = syncStatus=="ok"`).
- `Api/Features/Processes/ProcessesEndpoints.cs` con `MapGroup("/api/v1/processes")`
  `.RequireAuthorization().WithTags("Processes")`, registrado en `Program.cs`.
- Paginación: default `page=1, pageSize=20`, `pageSize` cap a 100.

## 4. PR2 — Escritura (`feature/process-write-backend`)

- `ProcessCreationService` (núcleo compartido) orquesta el flujo síncrono del
  blueprint §5: validar radicado → 409 `IMPORT_IN_PROGRESS` si hay import activo →
  409 `DUPLICATE_PROCESS` si existe `(user_id,file_number)` → overview (null ⇒ 422
  `PROCESS_NOT_FOUND_IN_RAMA`) → detail/subjects/actions → persistir en **una
  transacción** (`attended=true`, `sync_status="ok"`, `sync_phase="idle"`).
  **Partial:** fallo post-overview ⇒ persistir parcial, `sync_status="partial"`,
  `sync_phase="pending_partial_completion"`, `IPartialFetchScheduler.Schedule(...)`,
  201 con `syncStatus="partial"`. WAF se trata como partial (cooldown global es de 3.C).
- Commands: `CreateProcessFromFileNumberCommand` (`POST /processes/full-number`),
  `CreateProcessFromWizardCommand` (`POST /processes/wizard`, compone radicado
  `official_code(12)+year(4)+consecutive→pad7`, valida court↔city, delega al núcleo),
  `MarkAttendedCommand` (`POST /processes/{id}/mark-attended`, idempotente),
  `SoftDeleteProcessCommand` (`DELETE /processes/{id}`, `is_active=false`).
- `IProcessRepository` se extiende (aditivo) con helpers de escritura/unicidad/owner.

## 5. Testing

- **Unit** (`Application.UnitTests/Features/Processes/`): un test por handler con
  fakes NSubstitute (`IProcessReader`, `ICurrentUserService`, `IRamaJudicialClient`,
  `IDateTimeProvider`, `IPartialFetchScheduler`). Casos: ok/partial/422/409×2/wizard-pad/
  idempotencia/ownership/filtros/paginación.
- **Integración** (`Api.IntegrationTests/Features/Processes/`): Testcontainers Postgres
  (reusa `ApiFactory`), `IRamaJudicialClient` falso en el factory (cero hits a la API
  con WAF), `TestAuthHandler` emite claim `sub` para resolver ownership. Por endpoint:
  401/200-201/edge.

## 6. Restricciones respetadas

Sin migración nueva; sin tocar Auth/Sync/Imports/Frontend/seed/migrations. Clean
Architecture (handlers en Application, EF en Infrastructure), `AsNoTracking`,
`CancellationToken`, records para DTOs, `TreatWarningsAsErrors`, CQRS sin MediatR, TDD.

## 7. Entregables

- **PR1** `feature/process-read-backend`: `feat(api): process read endpoints — task 2.B`.
- **PR2** `feature/process-write-backend`: `feat(api): process write endpoints — task 2.B`.
