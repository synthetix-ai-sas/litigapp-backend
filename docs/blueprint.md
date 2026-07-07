# LitigApp — Blueprint de Construcción

> Generado por The Architect — 2026-05-25
> Arquetipo: SaaS Web + Mobile (vertical legaltech, Colombia)
> Audiencia: una instancia de Claude Code construyendo desde cero

---

## 0. Cómo usar este documento

Este blueprint es **autosuficiente**. Un agente de desarrollo con cero contexto previo debe poder leer este archivo y construir LitigApp de principio a fin sin tener que volver a preguntarle al usuario decisiones de arquitectura.

- **Lee en orden**: secciones 1-10 son contexto y arquitectura. Sección 11 (Build Order) es el script paso a paso.
- **Antes de empezar**: copia la sección 18 (`CLAUDE.md`) al root del proyecto target.
- **Cuando una sección diga "TBD spike"**: hay UNA cosa que necesita validarse contra la API real al inicio del desarrollo. Está claramente marcada.
- **Reglas No Negociables (sección 19)** son inviolables. Si encuentras una razón fuerte para romperlas, detén el desarrollo y consulta al usuario.

---

## 1. Visión General del Proyecto

### Qué es
LitigApp es una aplicación SaaS web + móvil para abogados litigantes en Colombia. Monitorea automáticamente el estado de procesos judiciales consultando la API pública de la Rama Judicial y notifica al abogado por email y WhatsApp cuando un proceso cambia de estado.

### Problema que resuelve
Un abogado promedio maneja 50+ procesos simultáneos. Hoy los rastrea en Excel, llamando al juzgado o yendo en persona. Pierde fallos, vencimientos y actuaciones críticas. LitigApp elimina ese trabajo manual.

### Objetivos del MVP
- Monitoreo automático diario de procesos judiciales por API.
- Notificación por **email** cuando hay novedad (digest agregado por usuario).
- Dashboard con pestañas Novedades / Procesos, filtros y paginación.
- Importación de portafolio existente desde Excel con mapeo de columnas.
- Creación manual de procesos con o sin radicado completo.
- Descarga de PDF con la información actual del proceso.
- **Tolerancia a bloqueo WAF de la API Rama Judicial** mediante sync engine self-healing.

### Métricas de éxito iniciales
- 3-10 abogados activos en las primeras 4 semanas post-lanzamiento.
- 0 procesos perdidos por falla del sistema (cobertura del sync).
- Notificaciones llegando en < 1 hora desde que la API expone la novedad.
- Carga de portafolio inicial < 5 minutos para 100 procesos.

### Fuera del alcance del MVP
- Pagos / pasarela de suscripción (cobro manual por ahora)
- **Notificaciones por WhatsApp** (pospuesto a v2 — interfaz `IWhatsAppSender` queda lista en el código pero sin implementación activa)
- Push notifications nativas (email cubre)
- Multi-usuario por firma (1 cuenta = 1 abogado)
- Subida de documentos del proceso
- IA / resúmenes / predicción de fallo
- Internacionalización (solo es-CO)
- Proxy rotativo (BrightData) — preparado arquitectónicamente, se activa en Tier 2

---

## 2. Tech Stack

### Backend
| Capa | Tecnología | Versión | Razón |
|---|---|---|---|
| Runtime | .NET | 10 LTS | LTS vigente (lanzada nov 2025), soporte hasta nov 2028 |
| Lenguaje | C# | 14 | Estable, viene con .NET 10 |
| Web framework | ASP.NET Core Web API | 10 | Estándar industria |
| ORM | Entity Framework Core | 10 | Productividad + migrations nativas |
| BD | PostgreSQL | 16+ | Free en Supabase, escalable, JSON nativo |
| Auth | ASP.NET Core Identity + JWT Bearer | 10 | Control total, gratis, integrado con EF |
| Background jobs | Hangfire | 1.8+ | Persistencia en Postgres, dashboard incluido |
| Resiliencia HTTP | Polly | 8+ | Retry, circuit breaker, rate limiter, bulkhead |
| HTTP client | HttpClientFactory + Polly | — | Manejo limpio de timeouts y retries |
| Validación | FluentValidation | 11+ | Validadores tipados desacoplados de DTOs |
| Mapping | Mapster | 7+ | Más liviano que AutoMapper, sin reflexión en hot path |
| Excel | ClosedXML | 0.104+ | Lectura/escritura .xlsx sin Interop |
| PDF | QuestPDF | 2024+ | API fluent, sin licencia comercial bajo Community para nuestro uso |
| Logging | Serilog | 4+ | Sink a consola, file y Logtail/Better Stack |
| Tests | xUnit + FluentAssertions + Testcontainers | — | Postgres real en integration tests |
| CQRS handlers | Custom (sin MediatR) | — | MediatR pasó a licencia comercial; usamos handlers propios |
| Email | Resend (.NET SDK no oficial o HttpClient directo) | — | 3K free/mes, API simple |
| WhatsApp | **FUERA DEL MVP** — solo interfaz `IWhatsAppSender` queda lista | — | Decisión: arrancar email-only. Costos Meta (~$15-30/mes a 100 abogados) + complejidad templates aprobados se posponen a evolutivo v2 |

### Frontend Web
| Capa | Tecnología | Versión | Razón |
|---|---|---|---|
| Framework | Angular | 20+ standalone | Versión actual estable con signals consolidados, zoneless |
| Lenguaje | TypeScript | strict | Único modo aceptable |
| Estilos | Tailwind CSS | v4 | Match exacto con el mockup |
| Iconos | lucide-angular | latest | Equivalente Angular a lucide-react del mockup |
| Estado | Angular Signals + RxJS | — | Suficiente, sin NgRx (over-engineering a esta escala) |
| HTTP | Angular HttpClient + interceptors | — | Estándar |
| Forms | Reactive Forms | — | Tipados, validación robusta |
| Routing | Angular Router (standalone API) | — | Estándar |
| Tests unit | Vitest o Jest | — | Vitest preferido por velocidad |
| Tests E2E | Playwright | latest | Flows críticos: login, importar, atender novedad |
| Package manager | pnpm | latest | Rápido, deterministic |
| Mobile wrapper | Capacitor | 6+ | Empaqueta Angular como iOS/Android. SIN Ionic. |
| PWA | @angular/pwa | — | Service worker + manifest |

### Infraestructura
| Servicio | Proveedor | Plan inicial | Costo/mes inicial |
|---|---|---|---|
| Hosting frontend | Vercel | Hobby | $0 |
| Hosting backend | Railway | Hobby → Pro | $5 → $20 |
| BD Postgres | Supabase | Free → Pro | $0 → $25 |
| Email | Resend | Free | $0 (3K msgs/mes) |
| WhatsApp | **Fuera del MVP** | — | $0 — pospuesto a v2 |
| DNS / dominio | Cualquier registrar | — | ~$15/año |
| Logs | Consola + Logtail Free | — | $0 |
| Monitoring errores | Sentry Free | — | $0 |
| **Total Tier 0 (MVP)** | | | **~$5-10/mes** |

---

## 3. Estructura de Directorios

### 3.1 Backend (LitigApp.sln — Clean Architecture + Feature Folders)

```
litigapp-backend/
├── LitigApp.sln
├── Directory.Build.props                       # nullable enable, langversion, treat warnings as errors
├── docker-compose.yml                          # Postgres local + (opcional) Seq para logs
├── README.md
├── .editorconfig
├── .gitignore
├── src/
│   ├── LitigApp.Domain/                        # csproj — sin dependencias externas
│   │   ├── Common/
│   │   │   ├── BaseEntity.cs                   # Id (Guid), CreatedAt
│   │   │   ├── DomainEvent.cs                  # IDomainEvent + IEntityWithEvents
│   │   │   ├── Result.cs                       # Result<T> con Success/Failure tipado
│   │   │   ├── ValueObject.cs
│   │   │   └── Errors/                         # DomainError, ValidationError
│   │   ├── Processes/
│   │   │   ├── Process.cs                      # entidad principal
│   │   │   ├── ProcessAction.cs                # actuaciones
│   │   │   ├── ProcessSubject.cs               # sujetos procesales
│   │   │   ├── FileNumber.cs                   # value object: validación de 23 dígitos + descomposición
│   │   │   ├── ProcessStatus.cs                # enum: Pending, Synced, Error, NotFound, Blocked
│   │   │   ├── Events/
│   │   │   │   └── ProcessUpdatedEvent.cs
│   │   │   └── Errors/                         # ProcessNotFound, DuplicateProcess, etc.
│   │   ├── Catalog/
│   │   │   ├── Department.cs
│   │   │   ├── City.cs
│   │   │   ├── Entity.cs                       # Entidad de la Rama Judicial
│   │   │   ├── Specialty.cs                    # Especialidad
│   │   │   └── Court.cs                        # Despacho
│   │   ├── Users/
│   │   │   ├── User.cs                         # dominio puro, NO Identity
│   │   │   └── NotificationPreferences.cs
│   │   ├── Notifications/
│   │   │   ├── OutboxMessage.cs
│   │   │   ├── NotificationChannel.cs          # enum: Email, WhatsApp
│   │   │   ├── NotificationStatus.cs           # enum: Pending, Processing, Sent, Failed
│   │   │   └── NotificationLog.cs
│   │   └── Imports/
│   │       ├── ImportJob.cs
│   │       └── ImportJobStatus.cs
│   │
│   ├── LitigApp.Application/                   # csproj — referencia: Domain
│   │   ├── Common/
│   │   │   ├── Abstractions/
│   │   │   │   ├── ICommand.cs
│   │   │   │   ├── IQuery.cs
│   │   │   │   ├── ICommandHandler.cs          # Task<Result<TResponse>> Handle(TCommand, CancellationToken)
│   │   │   │   ├── IQueryHandler.cs
│   │   │   │   ├── IUnitOfWork.cs
│   │   │   │   ├── IProcessRepository.cs
│   │   │   │   ├── IUserRepository.cs
│   │   │   │   ├── ICourtRepository.cs
│   │   │   │   ├── IOutboxRepository.cs
│   │   │   │   ├── IRamaJudicialClient.cs      # contrato para infra
│   │   │   │   ├── IEmailSender.cs
│   │   │   │   ├── IWhatsAppSender.cs
│   │   │   │   ├── IPdfGenerator.cs
│   │   │   │   ├── IExcelParser.cs
│   │   │   │   ├── IJwtTokenService.cs
│   │   │   │   ├── IDateTimeProvider.cs        # para testabilidad
│   │   │   │   └── ICurrentUserService.cs
│   │   │   ├── Behaviors/
│   │   │   │   ├── ValidationBehavior.cs       # ejecutado por DI decorator
│   │   │   │   └── LoggingBehavior.cs
│   │   │   ├── Pagination/
│   │   │   │   ├── PagedResult.cs
│   │   │   │   └── PaginationRequest.cs
│   │   │   └── Exceptions/
│   │   ├── Features/
│   │   │   ├── Auth/
│   │   │   │   ├── Commands/Register/          # RegisterCommand + Handler + Validator
│   │   │   │   ├── Commands/Login/
│   │   │   │   ├── Commands/RefreshToken/
│   │   │   │   ├── Commands/RequestPasswordReset/
│   │   │   │   └── Commands/ResetPassword/
│   │   │   ├── Processes/
│   │   │   │   ├── Commands/CreateProcessByFullNumber/
│   │   │   │   ├── Commands/CreateProcessByWizard/
│   │   │   │   ├── Commands/MarkAsAttended/
│   │   │   │   ├── Commands/DeleteProcess/
│   │   │   │   ├── Queries/GetProcessDetail/
│   │   │   │   ├── Queries/ListNovelties/      # pestaña Novedades
│   │   │   │   └── Queries/ListProcesses/      # pestaña Procesos (con filtros)
│   │   │   ├── Catalog/
│   │   │   │   ├── Queries/ListDepartments/
│   │   │   │   ├── Queries/ListCitiesByDepartment/
│   │   │   │   ├── Queries/ListSpecialties/
│   │   │   │   └── Queries/ListCourtsByCity/
│   │   │   ├── Imports/
│   │   │   │   ├── Commands/PreviewImportFile/      # parsea Excel, devuelve preview + columnas
│   │   │   │   ├── Commands/ExecuteImport/          # con mapeo confirmado
│   │   │   │   └── Queries/GetImportJobStatus/
│   │   │   ├── Notifications/
│   │   │   │   ├── Commands/UpdatePreferences/
│   │   │   │   └── Queries/GetPreferences/
│   │   │   └── Pdf/
│   │   │       └── Queries/GenerateProcessPdf/
│   │   └── DependencyInjection.cs              # AddApplication() — registra handlers, validators, mapster
│   │
│   ├── LitigApp.Infrastructure/                # csproj — referencia: Application, Domain
│   │   ├── Persistence/
│   │   │   ├── AppDbContext.cs
│   │   │   ├── Configurations/                 # IEntityTypeConfiguration<T> por entidad
│   │   │   │   ├── ProcessConfiguration.cs
│   │   │   │   ├── ProcessActionConfiguration.cs
│   │   │   │   ├── ProcessSubjectConfiguration.cs
│   │   │   │   ├── CourtConfiguration.cs
│   │   │   │   ├── OutboxMessageConfiguration.cs
│   │   │   │   └── ...
│   │   │   ├── Migrations/                     # generadas por EF Core
│   │   │   ├── Repositories/
│   │   │   │   ├── ProcessRepository.cs
│   │   │   │   ├── CourtRepository.cs
│   │   │   │   ├── OutboxRepository.cs
│   │   │   │   └── ...
│   │   │   ├── UnitOfWork.cs
│   │   │   └── Interceptors/
│   │   │       └── AuditInterceptor.cs         # llena CreatedAt/UpdatedAt automático
│   │   ├── Identity/
│   │   │   ├── ApplicationUser.cs              # extiende IdentityUser, mapea a User dominio
│   │   │   ├── IdentityDbContext.cs            # o compartido con AppDbContext (decisión: compartido)
│   │   │   ├── JwtTokenService.cs
│   │   │   ├── CurrentUserService.cs
│   │   │   └── PasswordHasherOptions.cs
│   │   ├── ExternalApis/
│   │   │   └── RamaJudicial/
│   │   │       ├── RamaJudicialClient.cs       # implementa IRamaJudicialClient
│   │   │       ├── RamaJudicialOptions.cs      # base URL, timeouts, rate limits
│   │   │       ├── PollyPolicies.cs            # retry + circuit breaker + rate limiter + bulkhead
│   │   │       ├── Dtos/                       # tipados exactos según API real
│   │   │       │   ├── OverviewResponse.cs
│   │   │       │   ├── ProcessDetailResponse.cs
│   │   │       │   ├── SubjectsResponse.cs
│   │   │       │   └── ActionsResponse.cs
│   │   │       └── Mappers/                    # Dto → Domain
│   │   ├── Notifications/
│   │   │   ├── Email/
│   │   │   │   ├── ResendEmailSender.cs        # implementa IEmailSender
│   │   │   │   ├── ResendOptions.cs
│   │   │   │   └── Templates/                  # plantillas HTML inline
│   │   │   │       └── ProcessUpdatedTemplate.cs
│   │   │   └── WhatsApp/
│   │   │       ├── MetaCloudWhatsAppSender.cs  # implementa IWhatsAppSender
│   │   │       ├── MetaCloudOptions.cs
│   │   │       └── Templates/                  # nombres de templates aprobados en Meta
│   │   ├── Pdf/
│   │   │   ├── QuestPdfProcessGenerator.cs     # implementa IPdfGenerator
│   │   │   └── Documents/
│   │   │       └── ProcessReportDocument.cs    # IDocument de QuestPDF
│   │   ├── Excel/
│   │   │   ├── ClosedXmlExcelParser.cs         # implementa IExcelParser
│   │   │   └── ColumnMappingValidator.cs
│   │   ├── Outbox/
│   │   │   └── OutboxMessagePublisher.cs       # inserta en notifications_outbox (placeholder para broker real)
│   │   ├── Time/
│   │   │   └── SystemDateTimeProvider.cs
│   │   └── DependencyInjection.cs              # AddInfrastructure(IConfiguration)
│   │
│   ├── LitigApp.Jobs/                          # csproj — referencia: Application, Infrastructure
│   │   ├── ProcessSyncJobs/
│   │   │   ├── OverviewSweepJob.cs             # RecurringJob cada Sweep.OverviewIntervalMinutes (default 15min) — solo overview
│   │   │   ├── ActionsSweepJob.cs              # encolado por OverviewSweep — actuaciones pág.1 de los procesos con cambios
│   │   │   └── CompletePartialFetchJob.cs      # encolado tras creación/import parcial — completa endpoints faltantes
│   │   ├── NotificationJobs/
│   │   │   ├── DispatchUserNotificationsJob.cs # triggered por sync — 1 email digest por usuario
│   │   │   ├── DispatchImportCompleteJob.cs    # triggered al terminar un import
│   │   │   └── NotificationFallbackSweepJob.cs # RecurringJob cada hora — recoge outbox pendiente (NO cada 5 min)
│   │   ├── ImportJobs/
│   │   │   └── BulkImportJob.cs                # encolado por POST /imports — procesa Excel
│   │   ├── MaintenanceJobs/
│   │   │   ├── OutboxCleanupJob.cs             # semanal — borra outbox 'sent' > 30 días
│   │   │   └── ImportJobsCleanupJob.cs         # semanal — borra import_jobs > 90 días
│   │   ├── HangfireConfiguration.cs            # recurring jobs registration
│   │   └── DependencyInjection.cs              # AddJobs(IConfiguration)
│   │
│   └── LitigApp.Api/                           # csproj — referencia: Application, Infrastructure, Jobs
│       ├── Features/                           # endpoints por feature (Minimal APIs con MapGroup)
│       │   ├── Auth/AuthEndpoints.cs           # MapGroup("/api/v1/auth")
│       │   ├── Processes/ProcessesEndpoints.cs # MapGroup("/api/v1/processes")
│       │   ├── Catalog/CatalogEndpoints.cs     # MapGroup("/api/v1/catalog")
│       │   ├── Imports/ImportsEndpoints.cs
│       │   ├── Notifications/NotificationsEndpoints.cs
│       │   ├── Pdf/PdfEndpoints.cs
│       │   └── Health/HealthEndpoints.cs
│       ├── Middleware/
│       │   ├── ExceptionHandlingMiddleware.cs
│       │   └── RequestLoggingMiddleware.cs
│       ├── Filters/
│       │   └── ValidationFilter.cs
│       ├── Auth/
│       │   ├── JwtAuthExtensions.cs
│       │   └── AuthorizationPolicies.cs
│       ├── Swagger/
│       │   └── SwaggerExtensions.cs
│       ├── Hangfire/
│       │   └── HangfireDashboardAuthFilter.cs
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       ├── Program.cs
│       └── Properties/launchSettings.json
│
└── tests/
    ├── LitigApp.Domain.UnitTests/
    │   └── Processes/FileNumberTests.cs
    ├── LitigApp.Application.UnitTests/
    │   └── Features/Processes/...HandlerTests.cs
    └── LitigApp.Api.IntegrationTests/
        ├── Common/TestcontainersPostgresFixture.cs
        ├── Common/ApiFactory.cs
        └── Features/Processes/ProcessesEndpointsTests.cs
```

**Regla de dependencias** (Clean Architecture, no se rompe):
```
Domain        ←  nada
Application   ←  Domain
Infrastructure←  Application + Domain
Jobs          ←  Application + Infrastructure + Domain
Api           ←  Application + Infrastructure + Jobs + Domain (infra/jobs solo para DI)
```

### 3.2 Frontend (Angular + capas por carpeta + ESLint boundaries)

```
litigapp-web/
├── package.json
├── pnpm-lock.yaml
├── angular.json
├── tsconfig.json                               # strict: true, noImplicitOverride, etc.
├── tailwind.config.js
├── postcss.config.js
├── capacitor.config.ts
├── eslint.config.js                            # con no-restricted-imports para boundaries
├── README.md
├── .gitignore
├── public/
│   ├── manifest.webmanifest
│   ├── icons/
│   └── favicon.ico
├── ios/                                        # generado por capacitor (gitignored opcionalmente)
├── android/                                    # idem
└── src/
    ├── main.ts
    ├── index.html
    ├── styles.css                              # Tailwind v4: @import "tailwindcss"; + @theme con tokens (NO @tailwind base/components/utilities — eso es v3)
    ├── environments/
    │   ├── environment.ts                      # apiUrl placeholder
    │   └── environment.prod.ts
    └── app/
        ├── app.component.ts                    # solo <router-outlet>
        ├── app.config.ts                       # provideRouter, provideHttpClient, providers globales
        ├── app.routes.ts                       # rutas top-level con lazy loading
        ├── core/                               # singletons cargados una vez
        │   ├── auth/
        │   │   ├── auth.service.ts             # signals con currentUser, isAuthenticated
        │   │   ├── auth.guard.ts               # CanActivateFn
        │   │   ├── guest.guard.ts              # para login/register cuando ya hay sesión
        │   │   └── jwt.interceptor.ts          # añade Authorization, maneja 401 → refresh → reintenta
        │   ├── http/
        │   │   ├── api-client.ts               # wrapper tipado sobre HttpClient + error normalization
        │   │   └── api-error.ts
        │   └── config/
        │       └── app-config.ts               # readonly env helpers
        ├── shared/                             # cero estado, cero llamadas API
        │   ├── ui/
        │   │   ├── button/
        │   │   ├── modal/
        │   │   ├── badge/
        │   │   ├── input/
        │   │   ├── select/
        │   │   ├── pagination/
        │   │   ├── tabs/
        │   │   ├── card/
        │   │   ├── empty-state/
        │   │   └── icon/                       # wrapper de lucide-angular
        │   ├── util/
        │   │   ├── format-date.pipe.ts
        │   │   ├── format-file-number.pipe.ts  # formatea 23 dígitos a grupos legibles
        │   │   ├── truncate.pipe.ts
        │   │   └── helpers.ts
        │   └── domain/                         # solo types/interfaces
        │       ├── process.types.ts
        │       ├── user.types.ts
        │       ├── catalog.types.ts
        │       ├── notification.types.ts
        │       └── import.types.ts
        ├── data-access/                        # solo servicios HTTP, sin UI
        │   ├── processes.service.ts
        │   ├── catalog.service.ts
        │   ├── imports.service.ts
        │   ├── notifications.service.ts
        │   ├── auth.service.ts                 # registro, login, reset
        │   └── pdf.service.ts
        └── features/                           # cada feature autocontenida
            ├── auth/
            │   ├── login/
            │   │   ├── login.component.ts
            │   │   └── login.component.html
            │   ├── register/
            │   ├── forgot-password/
            │   └── reset-password/
            ├── dashboard/
            │   ├── dashboard.component.ts      # shell con tabs Novedades/Procesos + header
            │   ├── header/header.component.ts  # logo, badge notificaciones, avatar
            │   ├── novelties-tab/
            │   │   └── novelties-tab.component.ts
            │   └── processes-tab/
            │       └── processes-tab.component.ts
            ├── process-add/
            │   ├── add-process-dialog.component.ts   # modal con 2 tabs: manual / excel
            │   ├── full-number-form/                  # input directo de 23 dígitos
            │   └── wizard/                            # depto → municipio → tipo → despacho → últimos
            │       ├── wizard.component.ts
            │       ├── step-department.component.ts
            │       ├── step-city.component.ts
            │       ├── step-court.component.ts
            │       └── step-consecutive.component.ts
            ├── process-import/
            │   ├── upload-step.component.ts
            │   ├── mapping-step.component.ts          # mapeo de columnas
            │   ├── confirmation-step.component.ts
            │   └── progress-tracker.component.ts      # polling al ImportJob
            ├── process-detail/
            │   ├── detail-dialog.component.ts         # modal "Atender" / "Ver opciones"
            │   ├── action-history-list.component.ts   # actuaciones con grouping Auto+Fijación
            │   └── pdf-download-button.component.ts
            └── settings/
                ├── settings.component.ts
                ├── profile-form.component.ts
                └── notification-preferences-form.component.ts
```

**ESLint boundaries** (en `eslint.config.js`):

```js
// pseudocódigo — el agente generará la config real con la sintaxis ESM/CJS apropiada
rules: {
  'no-restricted-imports': ['error', {
    patterns: [
      // shared/ui NO importa data-access ni features
      { group: ['**/data-access/**', '**/features/**'], message: 'shared/ui debe ser puro' },
      // shared/util NO importa nada que no sea shared/util
      { group: ['**/data-access/**', '**/features/**', '**/core/**'], message: 'shared/util debe ser puro' },
      // features no se importan entre sí — solo a través de shared
      // (esta regla se especializa con overrides por subcarpeta)
    ]
  }]
}
```

---

## 4. Modelo de Datos

### 4.1 Esquema SQL completo (PostgreSQL)

```sql
-- =====================================================================
-- EXTENSIONES
-- =====================================================================
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";          -- búsqueda fuzzy en nombres de despacho

-- =====================================================================
-- USERS (gestionados por ASP.NET Identity + columnas custom)
-- Identity genera AspNetUsers / AspNetRoles / etc. Aquí solo las columnas custom
-- que extiende ApplicationUser. EF Core las crea automáticamente.
-- =====================================================================
-- AspNetUsers (gestionado por Identity)
--   Id (text), UserName, NormalizedUserName, Email, NormalizedEmail,
--   EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp,
--   PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnd,
--   LockoutEnabled, AccessFailedCount
-- Columnas custom en ApplicationUser:
--   FullName text NOT NULL,
--   WhatsAppPhone text,                          -- nullable. Capturado para v2; NO se usa en MVP
--   CreatedAt timestamptz NOT NULL DEFAULT now()

-- =====================================================================
-- CATÁLOGO GEOGRÁFICO Y JUDICIAL (precargado, raramente cambia)
-- =====================================================================

-- IMPORTANTE: los IDs de catálogo son códigos DANE oficiales (natural keys,
-- no surrogate keys). DANE los define como strings con ceros a la izquierda
-- ("05" Antioquia, "08" Atlántico, "17001" Manizales). Por eso usamos char(N)
-- de longitud fija, NO int/smallint. Si usas int, "05" se guarda como 5 y
-- pierdes información para componer el radicado.

CREATE TABLE departments (
  id char(2) PRIMARY KEY,                          -- código DANE: "05", "11", "17", ...
  name text NOT NULL
);

CREATE TABLE cities (
  id char(5) PRIMARY KEY,                          -- código DANE: "05001", "17001", "11001", ...
  department_id char(2) NOT NULL REFERENCES departments(id),
  name text NOT NULL
);
CREATE INDEX idx_cities_dept ON cities(department_id);

CREATE TABLE entities (
  code char(2) PRIMARY KEY,                        -- "71"
  name text NOT NULL                               -- "CENTRO DE SERVICIOS JUDICIALES"
);

CREATE TABLE specialties (
  code char(2) PRIMARY KEY,                        -- "03"
  name text NOT NULL                               -- "CIVIL"
);

CREATE TABLE courts (
  id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
  official_code char(12) NOT NULL UNIQUE,          -- codDespachoCompleto de la API
  city_id char(5) NOT NULL REFERENCES cities(id),
  entity_code char(2) REFERENCES entities(code),   -- nullable hasta validar contra catálogo
  specialty_code char(2) REFERENCES specialties(code),
  court_number smallint,
  name text NOT NULL,                              -- "JUZGADO 002 CIVIL MUNICIPAL DE..."
  is_active boolean NOT NULL DEFAULT true,
  raw_payload jsonb,
  last_synced_at timestamptz,
  created_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX idx_courts_city_spec ON courts(city_id, specialty_code);
CREATE INDEX idx_courts_name_trgm ON courts USING gin (name gin_trgm_ops);

-- =====================================================================
-- PROCESOS
-- =====================================================================

CREATE TABLE processes (
  id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
  user_id text NOT NULL,                           -- FK a AspNetUsers.Id
  file_number char(23) NOT NULL,                   -- radicado completo
  
  -- IDs externos (de la API Rama Judicial)
  external_process_id bigint,                      -- idProceso del overview
  external_connection_id integer,                  -- idConexion
  
  -- Metadata del proceso
  court_id uuid REFERENCES courts(id),
  filing_year smallint,
  process_type text,                               -- "De Ejecución"
  process_class text,                              -- "Ejecutivo Singular"
  process_subclass text,                           -- "Por sumas de dinero"
  resource text,                                   -- "Sin Tipo de Recurso"
  judge_name text,                                 -- ponente
  filing_content text,                             -- contenidoRadicacion
  is_private boolean NOT NULL DEFAULT false,
  custom_alias text,                               -- alias opcional del abogado
  
  -- Estado y sincronización
  current_status text,                             -- actuacion más reciente
  last_court_action_at timestamptz,                -- fechaUltimaActuacion (clave para sync)
  last_synced_at timestamptz,                      -- última sync EXITOSA del proceso
  last_sync_attempt_at timestamptz,                -- último intento (haya fallado o no) — para fairness
  last_external_consecutive integer NOT NULL DEFAULT 0,  -- max(consActuacion) almacenado
  sync_status text NOT NULL DEFAULT 'pending',     -- pending|ok|partial|error|not_found|blocked
  sync_phase text NOT NULL DEFAULT 'pending_initial_full',
  -- sync_phase values:
  --   'idle'                  → al día, esperando próximo ciclo
  --   'pending_overview'      → falta consultar overview en próximo OverviewSweep
  --   'pending_actions'       → overview detectó cambio, falta consultar actuaciones
  --   'pending_initial_full'  → proceso recién creado, falta fetch completo (detail+sujetos+actions)
  --   'pending_partial_completion' → creación quedó parcial por 403 o error, completar después
  sync_error text,
  sync_attempts integer NOT NULL DEFAULT 0,
  
  -- UX
  attended boolean NOT NULL DEFAULT true,
  
  -- Auditoría / soft delete
  is_active boolean NOT NULL DEFAULT true,
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now(),
  
  CONSTRAINT uq_processes_user_file UNIQUE (user_id, file_number),
  CONSTRAINT chk_processes_file_length CHECK (length(file_number) = 23)
);
CREATE INDEX idx_processes_user_attended ON processes(user_id, attended, last_court_action_at DESC);
CREATE INDEX idx_processes_user_active ON processes(user_id, is_active, last_court_action_at DESC);
CREATE INDEX idx_processes_sync_phase ON processes(sync_phase, last_sync_attempt_at NULLS FIRST)
  WHERE is_active = true;
CREATE INDEX idx_processes_external ON processes(external_process_id);

-- =====================================================================
-- SYNC_STATE — estado global del sync engine (WAF cooldown, etc.)
-- Tabla key-value. NO usar para datos de procesos, solo control global.
-- =====================================================================

CREATE TABLE sync_state (
  key text PRIMARY KEY,                            -- ej: 'waf_blocked_until', 'last_sweep_completed_at'
  value_text text,
  value_timestamp timestamptz,
  reason text,
  updated_at timestamptz NOT NULL DEFAULT now()
);

-- Filas iniciales seedeadas:
-- ('waf_blocked_until', null, null, null)
-- ('current_overview_throttle_seconds', '3', null, 'rate limit dinámico')
-- ('current_actions_throttle_seconds', '3', null, 'rate limit dinámico')

-- =====================================================================
-- ACTUACIONES (historial — solo página 1 de la API, ~40 más recientes)
-- =====================================================================

CREATE TABLE process_actions (
  id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
  process_id uuid NOT NULL REFERENCES processes(id) ON DELETE CASCADE,
  external_action_id bigint NOT NULL,              -- idRegActuacion (único en la API)
  consecutive_number integer NOT NULL,             -- consActuacion
  action_date date,                                -- fechaActuacion
  action text,                                     -- "Fijacion estado"
  annotation text,                                 -- "Actuación registrada el ..."
  term_start_date date,                            -- fechaInicial
  term_end_date date,                              -- fechaFinal
  recorded_at date,                                -- fechaRegistro (clave para agrupar Auto+Fijación)
  has_documents boolean NOT NULL DEFAULT false,
  rule_code text,                                  -- codRegla
  is_grouped_with uuid REFERENCES process_actions(id),  -- self-ref si es Fijación de un Auto previo
  raw_payload jsonb,
  created_at timestamptz NOT NULL DEFAULT now(),
  
  CONSTRAINT uq_actions_process_external UNIQUE (process_id, external_action_id)
);
CREATE INDEX idx_actions_process_consec ON process_actions(process_id, consecutive_number DESC);
CREATE INDEX idx_actions_process_recorded ON process_actions(process_id, recorded_at);

-- =====================================================================
-- SUJETOS PROCESALES (endpoint separado en la API)
-- =====================================================================

CREATE TABLE process_subjects (
  id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
  process_id uuid NOT NULL REFERENCES processes(id) ON DELETE CASCADE,
  external_subject_id bigint,                      -- idRegSujeto (null si fue ingreso manual)
  subject_type text NOT NULL,                      -- "Demandante" | "Demandado" | ...
  is_summoned boolean NOT NULL DEFAULT false,      -- esEmplazado
  identification text,
  name text NOT NULL,                              -- nombreRazonSocial
  source text NOT NULL DEFAULT 'api',              -- 'api' | 'manual'
  raw_payload jsonb,
  created_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX idx_subjects_process_type ON process_subjects(process_id, subject_type);

-- =====================================================================
-- NOTIFICATIONS OUTBOX (placeholder para broker real en el futuro)
-- =====================================================================

-- IMPORTANTE: una fila representa UN envío único a UN usuario por UN canal.
-- Si un usuario tiene 10 procesos actualizados en un mismo ciclo de sync,
-- se inserta UNA SOLA fila por canal con payload.processes = [10 items].
-- NO una fila por proceso. Esto evita spam y respeta UX del abogado.
CREATE TABLE notifications_outbox (
  id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
  user_id text NOT NULL,
  event_type text NOT NULL,                        -- 'UserProcessesUpdated' | 'ImportComplete' | 'WelcomeUser'
  channel text NOT NULL,                           -- 'email' | 'whatsapp'
  payload jsonb NOT NULL,                          -- contenido específico por event_type
  status text NOT NULL DEFAULT 'pending',          -- pending|processing|sent|failed
  attempts smallint NOT NULL DEFAULT 0,
  last_error text,
  created_at timestamptz NOT NULL DEFAULT now(),
  processed_at timestamptz
);
CREATE INDEX idx_outbox_status_created ON notifications_outbox(status, created_at)
  WHERE status IN ('pending', 'processing');

-- Payload shape para UserProcessesUpdated:
-- {
--   "processes": [
--     { "id": "uuid", "fileNumber": "17001...", "currentStatus": "Fijacion estado",
--       "lastActionDate": "2026-03-20", "courtName": "JUZGADO 002 ...",
--       "alias": "Cliente Pérez", "newActionsCount": 3 },
--     ...
--   ],
--   "totalProcessesChanged": 10,
--   "syncCycleAt": "2026-05-26T06:15:00Z"
-- }
--
-- Payload shape para ImportComplete:
-- { "importJobId": "uuid", "fileName": "portafolio.xlsx",
--   "totalRows": 87, "successCount": 82, "errorCount": 5, "completedAt": "..." }

CREATE TABLE notification_logs (
  id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
  outbox_id uuid REFERENCES notifications_outbox(id) ON DELETE SET NULL,
  user_id text NOT NULL,
  event_type text NOT NULL,
  channel text NOT NULL,
  process_ids uuid[],                              -- IDs de los procesos incluidos en el digest
  provider_message_id text,
  status text NOT NULL,                            -- delivered | bounced | failed
  sent_at timestamptz NOT NULL DEFAULT now(),
  raw_response jsonb
);
CREATE INDEX idx_notif_logs_user_sent ON notification_logs(user_id, sent_at DESC);

-- =====================================================================
-- IMPORT JOBS
-- =====================================================================

CREATE TABLE import_jobs (
  id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
  user_id text NOT NULL,
  file_name text NOT NULL,
  total_rows integer NOT NULL DEFAULT 0,
  processed_rows integer NOT NULL DEFAULT 0,
  success_count integer NOT NULL DEFAULT 0,
  error_count integer NOT NULL DEFAULT 0,
  status text NOT NULL DEFAULT 'pending',          -- pending|running|completed|failed
  column_mapping jsonb,                            -- v1: { radicadoCol: 'B', notesCol: 'I' } (radicado obligatorio, notas opcional)
  errors jsonb,                                    -- [{ row: 5, message: '...' }, ...]
  created_at timestamptz NOT NULL DEFAULT now(),
  completed_at timestamptz
);
CREATE INDEX idx_imports_user_created ON import_jobs(user_id, created_at DESC);

-- =====================================================================
-- PREFERENCIAS DE NOTIFICACIÓN
-- =====================================================================

CREATE TABLE user_notification_preferences (
  user_id text PRIMARY KEY,                        -- FK lógico a AspNetUsers.Id
  email_enabled boolean NOT NULL DEFAULT true,
  whatsapp_enabled boolean NOT NULL DEFAULT false, -- DESHABILITADO en MVP (WhatsApp fuera de alcance)
  quiet_hours_start time,
  quiet_hours_end time,
  updated_at timestamptz NOT NULL DEFAULT now()
);
-- NOTA: la columna whatsapp_enabled queda en el esquema preparada para v2.
-- En MVP el frontend NO muestra el toggle de WhatsApp (o lo muestra deshabilitado con "Próximamente").
```

**Hangfire** crea sus propias tablas (`hangfire.*` schema). No las gestionamos manualmente.

### 4.2 Entidades de Dominio (Domain layer)

Ejemplo de `Process.cs` (las demás siguen el mismo patrón):

```csharp
namespace LitigApp.Domain.Processes;

public sealed class Process : BaseEntity, IEntityWithEvents
{
    private readonly List<ProcessAction> _actions = new();
    private readonly List<ProcessSubject> _subjects = new();
    private readonly List<IDomainEvent> _events = new();

    public string UserId { get; private set; } = default!;
    public FileNumber FileNumber { get; private set; } = default!;

    public long? ExternalProcessId { get; private set; }
    public int? ExternalConnectionId { get; private set; }

    public Guid? CourtId { get; private set; }
    public short? FilingYear { get; private set; }
    public string? ProcessType { get; private set; }
    public string? ProcessClass { get; private set; }
    public string? ProcessSubclass { get; private set; }
    public string? Resource { get; private set; }
    public string? JudgeName { get; private set; }
    public string? FilingContent { get; private set; }
    public bool IsPrivate { get; private set; }
    public string? CustomAlias { get; private set; }

    public string? CurrentStatus { get; private set; }
    public DateTimeOffset? LastCourtActionAt { get; private set; }
    public DateTimeOffset? LastSyncedAt { get; private set; }
    public int LastExternalConsecutive { get; private set; }
    public ProcessSyncStatus SyncStatus { get; private set; } = ProcessSyncStatus.Pending;
    public string? SyncError { get; private set; }
    public int SyncAttempts { get; private set; }

    public bool Attended { get; private set; } = true;
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public IReadOnlyList<ProcessAction> Actions => _actions.AsReadOnly();
    public IReadOnlyList<ProcessSubject> Subjects => _subjects.AsReadOnly();
    public IReadOnlyList<IDomainEvent> DomainEvents => _events.AsReadOnly();

    private Process() { } // EF

    public static Process Create(string userId, FileNumber fileNumber, string? alias = null)
    {
        return new Process
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FileNumber = fileNumber,
            CustomAlias = alias,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void ApplyOverviewSync(OverviewSyncData data, DateTimeOffset now)
    {
        ExternalProcessId = data.ExternalProcessId;
        ExternalConnectionId = data.ExternalConnectionId;
        LastSyncedAt = now;
        SyncStatus = ProcessSyncStatus.Ok;
        SyncError = null;

        if (LastCourtActionAt != data.LastCourtActionAt)
        {
            LastCourtActionAt = data.LastCourtActionAt;
            // marca para deep sync — el job dispara la cola
        }
    }

    public void ApplyNewActions(IReadOnlyList<ProcessAction> newActions, DateTimeOffset now)
    {
        if (newActions.Count == 0) return;

        _actions.AddRange(newActions);
        LastExternalConsecutive = newActions.Max(a => a.ConsecutiveNumber);
        CurrentStatus = newActions.OrderByDescending(a => a.ConsecutiveNumber).First().Action;
        Attended = false;
        UpdatedAt = now;
        _events.Add(new ProcessUpdatedEvent(Id, UserId, newActions.Count));
    }

    public void MarkAsAttended(DateTimeOffset now)
    {
        if (Attended) return;
        Attended = true;
        UpdatedAt = now;
    }

    public void MarkSyncError(string error, DateTimeOffset now)
    {
        SyncStatus = ProcessSyncStatus.Error;
        SyncError = error;
        SyncAttempts++;
        LastSyncedAt = now;
    }

    public void ClearDomainEvents() => _events.Clear();
}
```

### Value Object `FileNumber.cs`

```csharp
namespace LitigApp.Domain.Processes;

public sealed record FileNumber
{
    public string Value { get; }
    public short DepartmentCode => short.Parse(Value[..2]);
    public string CityCode => Value[..5];
    public string CourtCode => Value[..12];
    public short FilingYear => short.Parse(Value[12..16]);
    public string ConsecutiveCode => Value[16..23];

    private FileNumber(string value) => Value = value;

    public static Result<FileNumber> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<FileNumber>.Failure("El radicado es requerido.");

        var cleaned = value.Trim();
        if (cleaned.Length != 23 || !cleaned.All(char.IsDigit))
            return Result<FileNumber>.Failure("El radicado debe tener exactamente 23 dígitos.");

        return Result<FileNumber>.Success(new FileNumber(cleaned));
    }

    /// <summary>
    /// Construye el radicado desde sus partes. El consecutivo se rellena con ceros a la derecha
    /// hasta completar 7 dígitos según especificación de la Rama Judicial.
    /// </summary>
    public static Result<FileNumber> Compose(
        string courtOfficialCode,    // 12 chars (positions 1-12)
        short filingYear,
        string consecutive)
    {
        if (courtOfficialCode is null || courtOfficialCode.Length != 12)
            return Result<FileNumber>.Failure("El código del despacho debe tener 12 dígitos.");

        if (filingYear < 1900 || filingYear > 2100)
            return Result<FileNumber>.Failure("Año de radicación inválido.");

        var cleanedConsec = (consecutive ?? "").Trim().TrimStart('0');
        if (cleanedConsec.Length == 0 || cleanedConsec.Length > 7 || !cleanedConsec.All(char.IsDigit))
            return Result<FileNumber>.Failure("Consecutivo inválido.");

        var paddedConsec = cleanedConsec.PadRight(7, '0');
        return Create($"{courtOfficialCode}{filingYear:D4}{paddedConsec}");
    }
}
```

---

## 5. API Design (Endpoints REST)

Convenciones:
- **Patrón**: Minimal APIs de .NET 10 (NO controllers MVC). Cada feature expone una clase estática `XxxEndpoints` con un método de extensión `MapXxxEndpoints(IEndpointRouteBuilder)` que usa `MapGroup` para agrupar rutas. Ver dotnet-toolkit:minimal-api skill.
- **Resultados**: usar `TypedResults` (no `IActionResult`). Permite OpenAPI más precisa y mejor testabilidad.
- **OpenAPI**: nativo de .NET 10 (`AddOpenApi()` + `MapOpenApi()`). NO Swashbuckle. Ver dotnet-toolkit:openapi skill.
- **Errores**: ProblemDetails (RFC 9457) vía `TypedResults.Problem(...)` o handler global. Ver dotnet-toolkit:error-handling skill.
- **Validación**: FluentValidation invocado en filters de endpoint o en handler. 400 con ProblemDetails de tipo `validation`.
- Base path: `/api/v1`
- Auth: JWT Bearer en `Authorization: Bearer <token>` para todo excepto `/auth/*` y `/health`. Aplicar con `.RequireAuthorization()` en el `MapGroup`.
- Paginación: `?page=1&pageSize=20` con respuesta `{ items, total, page, pageSize, totalPages }`
- **Sin envelope**: las respuestas exitosas devuelven el DTO/recurso **directamente** (o `{ items, total, ... }` en listados); los errores usan **ProblemDetails**. ⚠️ Algunos ejemplos JSON de esta sección todavía muestran un sobre `{ "data": ..., "error": null }` — ese sobre está **DESACTUALIZADO, no se implementa; ignorarlo**. El backend real no lo usa (usa ProblemDetails para errores).
- **Nombres consistentes**: usar `totalRows` (no `rowCount`) para conteos de filas, alineado con `import_jobs` y los estados de job.

**Patrón de endpoint** (ejemplo):

```csharp
public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/catalog")
            .RequireAuthorization()
            .WithTags("Catalog");

        group.MapGet("/departments", ListDepartments)
            .WithName("ListDepartments")
            .WithSummary("Lista todos los departamentos");

        return app;
    }

    private static async Task<Ok<List<DepartmentDto>>> ListDepartments(
        IQueryHandler<ListDepartmentsQuery, List<DepartmentDto>> handler,
        CancellationToken ct)
    {
        var result = await handler.Handle(new ListDepartmentsQuery(), ct);
        return TypedResults.Ok(result.Value);
    }
}
```

En `Program.cs`: `app.MapAuthEndpoints().MapCatalogEndpoints().MapProcessesEndpoints()...`

### Routes Overview

| Método | Path | Descripción | Auth |
|---|---|---|---|
| POST | `/api/v1/auth/register` | Crear cuenta | No |
| POST | `/api/v1/auth/login` | Login → JWT | No |
| POST | `/api/v1/auth/refresh` | Refrescar token | No (refresh token) |
| POST | `/api/v1/auth/password-reset/request` | Solicitar email reset | No |
| POST | `/api/v1/auth/password-reset/confirm` | Confirmar nuevo password | No |
| GET | `/api/v1/processes/novelties` | Lista pestaña Novedades (attended=false) | Sí |
| GET | `/api/v1/processes` | Lista pestaña Procesos con filtros | Sí |
| GET | `/api/v1/processes/{id}` | Detalle de un proceso (incluye actuaciones y sujetos) | Sí |
| POST | `/api/v1/processes/full-number` | Crear proceso con radicado de 23 dígitos | Sí |
| POST | `/api/v1/processes/wizard` | Crear proceso con partes (depto/ciudad/despacho/año/consec) | Sí |
| POST | `/api/v1/processes/{id}/mark-attended` | Marca como atendido | Sí |
| DELETE | `/api/v1/processes/{id}` | Soft delete | Sí |
| GET | `/api/v1/processes/{id}/pdf` | Descarga PDF generado | Sí |
| GET | `/api/v1/catalog/departments` | Lista departamentos | Sí |
| GET | `/api/v1/catalog/departments/{id}/cities` | Lista ciudades | Sí |
| GET | `/api/v1/catalog/specialties` | Lista especialidades | Sí |
| GET | `/api/v1/catalog/cities/{cityId}/courts` | Lista despachos por ciudad (opcional ?specialtyId=) | Sí |
| POST | `/api/v1/imports/preview` | Sube Excel, devuelve preview + columnas detectadas | Sí (multipart/form-data) |
| POST | `/api/v1/imports` | Ejecuta importación con mapeo confirmado | Sí |
| GET | `/api/v1/imports/{id}` | Estado del job de importación (polling) | Sí |
| GET | `/api/v1/imports/active` | Job activo del usuario (para bloquear UI) | Sí |
| GET | `/api/v1/notifications/preferences` | Obtiene preferencias | Sí |
| PUT | `/api/v1/notifications/preferences` | Actualiza preferencias | Sí |
| GET | `/api/v1/notifications/logs` | Historial de envíos | Sí |
| GET | `/api/v1/health` | Health check (sin auth, para Railway) | No |
| GET | `/hangfire` | Dashboard Hangfire (solo admin) | Sí + policy Admin |

### Endpoints clave — detalle

#### POST `/api/v1/processes/full-number` (creación SÍNCRONA)

```http
POST /api/v1/processes/full-number
Content-Type: application/json
Authorization: Bearer <jwt>

{
  "fileNumber": "17001400301020240019200",
  "alias": "Cliente Pérez - Ejecutivo" // opcional
}
```

**Flujo (síncrono, sin Hangfire)**:

1. Valida `fileNumber` (23 dígitos).
2. Verifica que NO haya `import_jobs` activo del usuario → si lo hay, **409 Conflict** con `error.code = 'IMPORT_IN_PROGRESS'`.
3. Verifica unicidad por `(user_id, file_number)` → si existe, **409 Conflict** con `error.code = 'DUPLICATE_PROCESS'`.
4. **Llama síncronamente a los 4 endpoints de la API Rama Judicial** (con Polly):
   - `GetOverviewByFileNumberAsync` → si retorna null → **422 Unprocessable** ("Proceso no encontrado en Rama Judicial").
   - `GetDetailAsync(externalProcessId)`.
   - `GetSubjectsAsync(externalProcessId)`.
   - `GetFirstPageActionsAsync(externalProcessId)`.
5. **Persiste todo en una sola transacción**: `processes` + `process_subjects` + `process_actions`.
6. Setea `attended = true` (es creación, no novedad pendiente).
7. Devuelve **201 Created** con el proceso completo (incluye sujetos y actuaciones).

**Fallback de robustez** — si algún call DESPUÉS del overview falla tras agotar reintentos de Polly:

- Persistimos lo obtenido hasta ese punto.
- Marcamos `sync_status = 'partial'` con `sync_error` describiendo qué faltó.
- Encolamos `CompletePartialFetchJob(processId)` para reintentar en background.
- Devolvemos **201 Created** con `syncStatus = 'partial'` para que el frontend muestre: "Proceso creado. Algunos datos se están terminando de cargar, recarga en unos minutos."

**Respuesta exitosa (201)** — sin envelope, el DTO directo:
```json
{
  "id": "uuid",
  "fileNumber": "17001400301020240019200",
  "syncStatus": "ok",
  "court": { "id": "...", "name": "JUZGADO 002 CIVIL..." },
  "processType": "De Ejecución",
  "currentStatus": "Fijacion estado",
  "lastCourtActionAt": "2026-03-20T00:00:00Z",
  "subjects": [...],
  "actions": [...],
  "createdAt": "2026-05-26T20:00:00Z"
}
```

**Por qué síncrono y no Hangfire**: el abogado espera ver el modal con la info del proceso al instante. 4 llamadas × 1-3s típicas = 4-12s aceptable con loading state. Encolar un job para 1 solo proceso sería complejidad innecesaria.

#### POST `/api/v1/processes/wizard` (también SÍNCRONO)

```json
{
  "cityId": 17001,
  "courtId": "uuid-del-despacho",
  "filingYear": 2024,
  "consecutive": "192",
  "alias": "Cliente Pérez - Ejecutivo"
}
```

Internamente compone el radicado: `courts.official_code (12) + filingYear (4) + consecutive padded a 7 = 23 dígitos`. Luego mismo flujo síncrono que `/full-number`.

#### GET `/api/v1/imports/active`

Permite al frontend saber si el usuario tiene una importación en curso para bloquear la UI:

```json
{
  "hasActive": true,
  "importJob": {
    "id": "uuid",
    "fileName": "portafolio.xlsx",
    "totalRows": 87,
    "processedRows": 34,
    "successCount": 0,
    "errorCount": 0,
    "status": "running",
    "errors": [],
    "createdAt": "...",
    "completedAt": null
  }
}
```
> Contrato canónico de `/imports/active`: **`{ hasActive: boolean, importJob: {...} | null }`** (sin envelope). Cuando no hay import en curso: `{ "hasActive": false, "importJob": null }`.

Si `hasActive = true`, el frontend deshabilita el botón "Agregar Proceso" con tooltip: "Hay una importación en curso. Espera a que termine antes de agregar procesos manualmente."

#### GET `/api/v1/processes/novelties`

```http
GET /api/v1/processes/novelties?page=1&pageSize=20
```

```json
{
  "items": [
    {
      "id": "uuid",
      "fileNumber": "17001400301020240019200",
      "currentStatus": "Fijacion estado",
      "lastCourtActionAt": "2026-03-20T00:00:00Z",
      "courtName": "JUZGADO 002 CIVIL MUNICIPAL DE EJECUCIÓN DE SENTENCIAS DE MANIZALES"
    }
  ],
  "total": 5, "page": 1, "pageSize": 20, "totalPages": 1
}
```

#### GET `/api/v1/processes` (con filtros)

```http
GET /api/v1/processes?page=1&pageSize=20
  &courtName=civil
  &fileNumber=17001
  &subjectName=perez
  &status=
  &fromDate=2026-01-01
  &toDate=2026-12-31
  &attended=true|false  (opcional, default = no filtra)
```

#### GET `/api/v1/processes/{id}` (detalle)

```json
{
  "id": "...", "fileNumber": "...", "alias": "...",
  "court": { "id": "...", "name": "...", "cityName": "Manizales", "departmentName": "Caldas" },
  "filingYear": 2024,
  "processType": "De Ejecución",
  "processClass": "Ejecutivo Singular",
  "judgeName": "...",
  "currentStatus": "...",
  "lastCourtActionAt": "...",
  "attended": false,
  "syncStatus": "ok",                   // "ok" | "partial" | "pending" | "error" | "not_found"
  "syncPhase": "idle",                  // info adicional para debugging
  "canDownloadPdf": true,               // false si syncStatus != 'ok'
  "subjects": [
    { "type": "Demandante", "name": "OSCAR ARTURO ORTIZ HENAO" },
    { "type": "Demandado", "name": "FRANCISCA HELENA GONZALEZ ARIAS" }
  ],
  "actions": [
    {
      "id": "...",
      "consecutiveNumber": 82,
      "actionDate": "2026-03-20",
      "action": "Fijacion estado",
      "annotation": "...",
      "termStartDate": "2026-03-24",
      "termEndDate": "2026-03-24",
      "groupedWithId": null
    }
  ]
}
```

**Manejo de estado `partial` en el frontend**:
- El detail dialog muestra un banner amarillo: "⏳ Estamos terminando de cargar los datos de este proceso. Recarga en unos minutos."
- Botón "Descargar PDF" **deshabilitado** con tooltip "Disponible cuando los datos terminen de cargar".
- Botón "Marcar atendido" **habilitado** (no requiere data completa).
- **Auto-refresh del dialog**: mientras `syncStatus='partial'`, polling al endpoint cada 10s. Cuando pase a `'ok'`, refresca y oculta el banner.

#### GET `/api/v1/processes/{id}/pdf` (con guard de syncStatus)

- Si el proceso tiene `sync_status != 'ok'` → **409 Conflict** con `error.code='PROCESS_DATA_INCOMPLETE'`.
- Si `sync_status='ok'` → genera y stream del PDF.

#### POST `/api/v1/imports/preview` (multipart/form-data)

```http
POST /api/v1/imports/preview
Content-Type: multipart/form-data
file=@portafolio.xlsx
```

**Respuesta**: muestra las primeras 5 filas y las columnas detectadas para que el frontend pida al usuario el mapeo.

```json
{
  "previewId": "tmp-uuid-cached-10min",
  "columns": [
    { "key": "A", "header": "DEPENDENCIA" },
    { "key": "B", "header": "NUMERO DE PROCESO" },
    { "key": "F", "header": "JUZGADO/CIUDAD" }
  ],
  "rows": [
    { "A": "PROMISCUOS", "B": "17873408900120240056300", "F": "Juzgado 01 Promiscuo Municipal - Caldas - Villamaría" }
  ],
  "totalRows": 51
}
```

#### POST `/api/v1/imports`

```json
{
  "previewId": "tmp-uuid-cached-10min",
  "mapping": {
    "fileNumberColumn": "A",
    "filingYearColumn": null,
    "cityColumn": "B",
    "courtColumn": "B",
    "consecutiveColumn": "A",
    "demandantColumn": "C",
    "demandadoColumn": "D",
    "aliasColumn": null
  },
  "fileNumberMode": "full" | "compose"   
  // "full" = la columna trae los 23 dígitos
  // "compose" = hay que construirlos a partir de otras columnas
}
```

**Respuesta**: 202 Accepted con `{ importJobId }`. Frontend hace polling a GET `/imports/{id}`.

---

## 6. Integración con API Rama Judicial

### 6.0 ⚠️ CONTEXTO CRÍTICO: WAF / Bloqueo por IP

**Hallazgo confirmado en pruebas con JMeter**:
- La API Rama Judicial está protegida por WAF (probablemente Cloudflare).
- Tras ~186 requests en ráfaga desde una misma IP, el WAF bloquea con **403 Forbidden**.
- El bloqueo dura aproximadamente **3-4 minutos**.
- El bloqueo es por IP. **Una sola IP de Railway = un solo "carril".**
- No verificado: si bombardeo sostenido durante días puede escalar a bloqueo permanente.

**Estrategia (capas en orden de aplicación)**:

| Capa | Mecanismo | Cuándo |
|---|---|---|
| 1 | Trickle controlado: 2-3s entre requests + jitter + User-Agent rotation | Siempre, desde día 1 |
| 2 | Self-healing: detección 403 → cooldown 15-20min → resume desde `pending_*` | Siempre, desde día 1 |
| 3 | Proxy rotativo (BrightData Web Unlocker u otro) | **Solo Tier 2+**, documentado, no activo en MVP |

**Reduce a 2 endpoints en el ciclo diario**: confirmado con negocio que `detail` y `subjects` **no cambian** después de la primera consulta. Por tanto:
- Ciclo diario consume SOLO `overview` + `actions` (mitad de carga).
- `detail` y `subjects` se consultan SOLO en creación inicial (manual, wizard, import).

### 6.1 Configuración (todo configurable, valores tunables)

`appsettings.json`:
```json
{
  "RamaJudicial": {
    "BaseUrl": "https://consultaprocesos.ramajudicial.gov.co:448",
    "TimeoutSeconds": 15,
    "MaxConcurrentRequests": 1,                    // 1 = serializado, anti-WAF
    "RetryCount": 3,
    "TransientFailureCircuitBreakerThreshold": 10,
    "TransientFailureCircuitBreakerDurationSeconds": 300,

    "Throttle": {
      "OverviewIntervalSecondsMin": 2,             // configurable, default 2s
      "OverviewIntervalSecondsMax": 3,             // jitter aleatorio en este rango
      "ActionsIntervalSecondsMin": 2,
      "ActionsIntervalSecondsMax": 3,
      "InitialFetchIntervalSecondsMin": 1,         // creación individual: usuario espera
      "InitialFetchIntervalSecondsMax": 2
    },

    "Waf": {
      "CooldownMinutesOnBlock": 20,                // cuánto esperar tras 403
      "ConsecutiveSuccessesToSpeedUp": 100,        // si 100 OK seguidos, bajar throttle
      "ConsecutiveBlocksToSlowDown": 1,            // 1 solo 403 ya nos hace ir más lento
      "EmergencyMaxThrottleSeconds": 10            // techo si todo va mal
    },

    "Sweep": {
      "OverviewIntervalMinutes": 15,               // cron del OverviewSweepJob (configurable)
      "BatchSize": 500,                            // procesos por corrida
      "MinimumHoursBetweenSyncsPerProcess": 22     // no resincronizar un proceso muy seguido
    },

    "Headers": {
      "AcceptLanguage": "es-ES,es;q=0.9",
      "Origin": "https://consultaprocesos.ramajudicial.gov.co",
      "Referer": "https://consultaprocesos.ramajudicial.gov.co/"
    },

    "UserAgentPool": [
      "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36",
      "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Safari/605.1.15",
      "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:128.0) Gecko/20100101 Firefox/128.0",
      "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36",
      "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36"
    ],

    "Proxy": {
      "Enabled": false,                            // ★ Tier 2+: poner true y configurar
      "Provider": "BrightData",                    // "BrightData" | "ScrapingBee" | "ScraperAPI"
      "Endpoint": "",
      "Username": "",
      "Password": ""
    }
  }
}
```

**Todas estas keys son configurables por env vars** vía la convención `RamaJudicial__Throttle__OverviewIntervalSecondsMin = "1"`. Permiten escalar sin redeploy: cuando lleguemos a 50+ abogados, bajar el throttle, aumentar batch, etc.

### 6.2 DTOs (basados en respuestas reales)

```csharp
namespace LitigApp.Infrastructure.ExternalApis.RamaJudicial.Dtos;

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
}

public sealed class Pagination
{
    public int CantidadRegistros { get; init; }
    public int RegistrosPagina { get; init; }
    public int CantidadPaginas { get; init; }
    public int Pagina { get; init; }
}
```

Configurar `JsonSerializerOptions` con `PropertyNamingPolicy = JsonNamingPolicy.CamelCase` para mapear desde la API que usa camelCase.

### 6.3 IRamaJudicialClient

```csharp
public interface IRamaJudicialClient
{
    Task<Result<OverviewProcess?>> GetOverviewByFileNumberAsync(string fileNumber, CancellationToken ct);
    Task<Result<ProcessDetailResponse?>> GetDetailAsync(long externalProcessId, CancellationToken ct);
    Task<Result<List<SubjectDto>>> GetSubjectsAsync(long externalProcessId, CancellationToken ct);
    Task<Result<List<ActionDto>>> GetFirstPageActionsAsync(long externalProcessId, CancellationToken ct);
}
```

**El `Result` debe distinguir 3 tipos de "fallo"**:

```csharp
public enum FailureKind
{
    None,
    NotFound,         // proceso no existe o radicado vacío
    WafBlocked,       // 403 Forbidden — propaga al job para cooldown
    Transient,        // 5xx, timeout — Polly ya hizo retry, sigue fallando
    InvalidInput      // 400 — radicado mal formado, etc.
}

public sealed record RamaJudicialFailure(FailureKind Kind, string Message);
```

**Casos especiales que el cliente debe manejar**:
- **403 Forbidden** → `FailureKind.WafBlocked`. **No reintentar con Polly**. Propagar arriba para que el job dispare cooldown.
- 200 con `procesos: []` y radicado válido (23 dígitos): retornar `Result.Success(null)` → caller decide si marcar `not_found` o esperar próximo sweep (puede ser bloqueo silencioso).
- 200 con array vacío en sujetos o actuaciones: retornar lista vacía (no es error).
- 404 con `"Message"`: parsear, retornar `Result.Success(null)` o `FailureKind.NotFound` según el mensaje.
- 500 con `"Object reference not set..."` o "Index was out of range": tratar como `FailureKind.Transient`. Polly reintenta.
- 400 con `"NumeroRadicacion ha de contener 23 digitos"` o `"Error converting data type"`: `FailureKind.InvalidInput`, no reintentar.

### 6.3.1 Headers + User-Agent rotation

Antes de cada request, el cliente debe:

```csharp
private void ApplyRotatingHeaders(HttpRequestMessage request)
{
    var pool = _options.UserAgentPool;
    var ua = pool[Random.Shared.Next(pool.Length)];

    request.Headers.UserAgent.ParseAdd(ua);
    request.Headers.Accept.ParseAdd("application/json, text/plain, */*");
    request.Headers.AcceptLanguage.ParseAdd(_options.Headers.AcceptLanguage);
    request.Headers.Add("Origin", _options.Headers.Origin);
    request.Headers.Add("Referer", _options.Headers.Referer);
    // sec-ch-ua y sec-fetch-* opcionales según UA elegido
}
```

Esto reduce la probabilidad de fingerprinting por WAF.

### 6.3.2 Decorator BrightData (preparado, NO activo en MVP)

Arquitectura preparada para Tier 2 sin reescribir nada:

```csharp
// Registro en DI condicional según config
if (options.Proxy.Enabled)
{
    services.AddHttpClient<DirectRamaJudicialClient>();
    services.AddHttpClient<BrightDataRamaJudicialClient>(/* config con proxy */);
    services.AddScoped<IRamaJudicialClient>(sp =>
        new FailoverRamaJudicialClient(
            primary: sp.GetRequiredService<DirectRamaJudicialClient>(),
            fallback: sp.GetRequiredService<BrightDataRamaJudicialClient>()));
}
else
{
    services.AddHttpClient<IRamaJudicialClient, DirectRamaJudicialClient>();
}
```

`FailoverRamaJudicialClient` usa el directo primero. Si en una corrida se acumulan N 403s, en el siguiente call usa el fallback (BrightData) para no quedar bloqueado. **En MVP `Proxy.Enabled=false`, no hay costo, solo está la infraestructura lista.**

### 6.4 Polly Policies

```csharp
public static class PollyPolicies
{
    public static IAsyncPolicy<HttpResponseMessage> BuildResiliencePolicy(RamaJudicialOptions options, ILogger logger)
    {
        var retry = HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(r => r.StatusCode == HttpStatusCode.RequestTimeout)
            .WaitAndRetryAsync(
                retryCount: options.RetryCount,
                sleepDurationProvider: attempt =>
                    TimeSpan.FromMilliseconds(200 * Math.Pow(5, attempt - 1)) + Jitter(),
                onRetry: (outcome, ts, attempt, _) =>
                    logger.LogWarning("Rama Judicial retry {Attempt} after {Delay}ms", attempt, ts.TotalMilliseconds));

        var circuit = HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: options.CircuitBreakerFailureThreshold,
                durationOfBreak: TimeSpan.FromSeconds(options.CircuitBreakerDurationSeconds),
                onBreak: (_, ts) => logger.LogError("Circuit OPEN for {Duration}s", ts.TotalSeconds),
                onReset: () => logger.LogInformation("Circuit closed"));

        var bulkhead = Policy.BulkheadAsync<HttpResponseMessage>(
            maxParallelization: options.MaxConcurrentRequests,
            maxQueuingActions: 100);

        var timeout = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(options.TimeoutSeconds));

        return Policy.WrapAsync(circuit, retry, bulkhead, timeout);
    }

    private static TimeSpan Jitter() => TimeSpan.FromMilliseconds(Random.Shared.Next(0, 200));
}
```

El rate limiter de 5 req/seg lo aplica un **`SemaphoreSlim`** con throttling propio (Polly v8 tiene rate limiter nativo si actualizas a `Microsoft.Extensions.Http.Resilience`).

### 6.5 Detección Auto + Fijación

Cuando se procesa la lista de actuaciones nuevas, ordenar por `consecutive_number` ASC. Recorrer y agrupar:

```csharp
foreach (var current in actionsAsc)
{
    var previous = actionsAsc[i - 1];
    var isFijacion = current.Action.StartsWith("Fijacion", OrdinalIgnoreCase);
    var sameRecordedDate = current.RecordedAt == previous.RecordedAt;
    var prevIsAuto = previous.Action.StartsWith("Auto", OrdinalIgnoreCase);

    if (isFijacion && sameRecordedDate && prevIsAuto)
        current.GroupedWithId = previous.Id;
}
```

Para notificaciones: agrupar por `recorded_at` y crear UN `OutboxMessage` por grupo (no uno por actuación).

---

## 7. Frontend Architecture

### 7.1 Rutas

```typescript
// app.routes.ts
export const routes: Routes = [
  // Públicas
  { path: 'login', loadComponent: () => import('./features/auth/login/login.component') },
  { path: 'register', loadComponent: () => import('./features/auth/register/register.component') },
  { path: 'forgot-password', loadComponent: () => import('./features/auth/forgot-password/forgot-password.component') },
  { path: 'reset-password', loadComponent: () => import('./features/auth/reset-password/reset-password.component') },
  
  // Autenticadas
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./features/dashboard/dashboard.component'),
    children: [
      { path: '', redirectTo: 'novelties', pathMatch: 'full' },
      { path: 'novelties', loadComponent: () => import('./features/dashboard/novelties-tab/novelties-tab.component') },
      { path: 'processes', loadComponent: () => import('./features/dashboard/processes-tab/processes-tab.component') },
      { path: 'settings', loadComponent: () => import('./features/settings/settings.component') },
    ]
  },
  { path: '**', redirectTo: '' }
];
```

### 7.2 State management

- **Estado local de componente**: `signal()` para todo (loading, items, filters).
- **Estado compartido**: servicios singleton con signals (ej: `AuthService.currentUser`, `ProcessesService.cachedNovelties`).
- **Comunicación con backend**: servicios en `data-access/` con métodos que retornan `Observable<T>` (RxJS sigue siendo el contrato natural de `HttpClient`).
- **Cacheo**: signals dentro del servicio + `tap()` para guardar la última respuesta.
- **Mutaciones optimistas**: en "Marcar como atendido", actualizar signal local antes de la respuesta, rollback si falla.

### 7.3 Composición de componentes (página Novedades)

```
<DashboardLayout>
  <Header>
    <Logo />
    <NotificationBadge count={signal} />
    <UserAvatar />
  </Header>
  <Tabs activeTab="novelties">
    <Tab name="novelties" badge={5}>Novedades</Tab>
    <Tab name="processes">Procesos</Tab>
  </Tabs>
  <NoveltiesTab>
    <PageHeader title="Procesos con nuevos estados" subtitle="..." />
    <Card>
      @if (loading()) {
        <SkeletonRows />
      } @else if (items().length === 0) {
        <EmptyState icon="check-circle" title="¡Al día!" />
      } @else {
        @for (proc of items(); track proc.id) {
          <NoveltyRow [process]="proc" (view)="openDetail(proc)" />
        }
        <Pagination ... />
      }
    </Card>
    @if (selected()) {
      <DetailDialog [process]="selected()" (attend)="markAttended()" (close)="selected.set(null)" />
    }
  </NoveltiesTab>
</DashboardLayout>
```

---

## 8. Design System

Extraído directamente del mockup aprobado (`litigapp_mockup.tsx`).

### Colores

| Token | Hex (Tailwind) | Uso |
|---|---|---|
| Primary 700 | `#1d4ed8` (blue-700) | Header background |
| Primary 600 | `#2563eb` (blue-600) | Botones primarios, hover headers tabs |
| Primary 500 | `#3b82f6` (blue-500) | Avatar background |
| Primary 50 | `#eff6ff` | Hover botones tab |
| Background | `#f8fafc` (slate-50) | Página |
| Surface | `#ffffff` | Cards, modales |
| Border | `#e2e8f0` (slate-200) | Bordes sutiles, divisores |
| Border-strong | `#cbd5e1` (slate-300) | Inputs |
| Text primary | `#1e293b` (slate-800) | Texto principal |
| Text secondary | `#475569` (slate-600) | Labels |
| Text muted | `#64748b` (slate-500) | Subtítulos, ayudas |
| Text disabled | `#94a3b8` (slate-400) | Iconos sin estado |
| Warning bg | `#fef3c7` (amber-100) | Badge "nuevo estado" |
| Warning text | `#92400e` (amber-800) | Texto del badge |
| Success bg | `#dcfce7` (green-50) | Badge "Atendido" |
| Success text | `#15803d` (green-700) | Idem |
| Success border | `#bbf7d0` (green-200) | Idem |
| Danger bg | `#fee2e2` (red-50) | Badge "Pendiente" |
| Danger text | `#b91c1c` (red-700) | Idem |
| Danger border | `#fecaca` (red-200) | Idem |
| Notification | `#ef4444` (red-500) | Badge contador rojo |

### Tipografía

- **Familia**: Inter, declarada en el bloque `@theme` de `styles.css` con `--font-sans: "Inter", ...` (Tailwind v4 es CSS-first; **no** hay `tailwind.config.js`).
- **Escalas**: `text-xs`, `text-sm`, `text-base`, `text-lg`, `text-xl`, `text-2xl`.
- **Pesos**: 400 (body), 500 (medium / labels), 600 (semibold / headings), 700 (bold / títulos grandes).

### Spacing & Layout

- **Base**: 4px (Tailwind default).
- **Border radius**:
  - `rounded` (4px) — badges pequeños
  - `rounded-md` (6px) — botones secundarios
  - `rounded-lg` (8px) — inputs, botones primarios
  - `rounded-xl` (12px) — cards, paneles
  - `rounded-2xl` (16px) — modales
  - `rounded-full` — avatares, notif badge, pills
- **Sombras**: `shadow-sm` para cards, `shadow-md` para header, `shadow-xl` para modales.
- **Max width**: `max-w-7xl` (1280px) centrado con `mx-auto px-4`.
- **Header height**: `h-16` (64px).
- **Breakpoints**: Tailwind defaults — `sm:640`, `md:768`, `lg:1024`, `xl:1280`.

### Animaciones

- Tabs y modales con `animate-in fade-in slide-in-from-bottom-2 duration-300` (instalar `tailwindcss-animate`).
- Modales: `zoom-in-95 duration-200`.
- Transiciones de color en hover: `transition-colors`.

### Componentes base requeridos

- `<app-button [variant]="'primary'|'secondary'|'ghost'|'danger'" [size]="...">` con loading state.
- `<app-modal [open]="signal" [size]="'sm'|'md'|'lg'" (close)="...">` con backdrop blur.
- `<app-badge [variant]="'warning'|'success'|'danger'|'info'">`.
- `<app-input [icon]="'building'" [label]="..." formControlName="...">`.
- `<app-select>` con búsqueda integrada (para depto/ciudad/despacho).
- `<app-pagination [page] [pageSize] [total] (change)>`.
- `<app-tabs>` con underline animado.
- `<app-card>` simple wrapper con border + shadow.
- `<app-empty-state [icon] [title] [description]>`.
- `<app-icon [name]="'bell'" [size]="20" [class]="...">` que es wrapper de lucide-angular.

---

## 9. Auth & Authorization

### 9.1 Flujo

1. **Registro**: POST `/auth/register` con `{ email, password, fullName, whatsappPhone?, acceptedTerms, acceptedPrivacy }` → valida que `acceptedTerms` y `acceptedPrivacy` sean `true` (si no, 422) → crea `AspNetUser`, registra la aceptación en `legal_acceptances` (versión + timestamp + IP) y envía email de confirmación (Resend). Ver §9.5.
2. **Login**: POST `/auth/login` con `{ email, password }` → valida con `SignInManager` → devuelve `{ accessToken (15 min), refreshToken (7 días) }`.
3. **Refresh**: POST `/auth/refresh` con `{ refreshToken }` → genera nuevo par.
4. **Reset password**: solicitar email → click en link con token → confirmar nuevo password.

### 9.2 JWT

- **Access token**: 15 min, claims `sub` (userId), `email`, `role`.
- **Refresh token**: 7 días, almacenado en tabla `refresh_tokens` (id, user_id, token_hash, expires_at, revoked_at).
- **Algoritmo**: HS256 con secret en `Jwt:Secret` (env var, mínimo 32 chars).
- **Validación**: `Issuer`, `Audience`, `Lifetime`, `SignatureKey` en `AddAuthentication().AddJwtBearer(...)`.

### 9.3 Roles

| Rol | Permisos |
|---|---|
| `User` | Default. CRUD sobre sus propios procesos. |
| `Admin` | + acceso al dashboard Hangfire, gestión catálogo. |

Asignar `Admin` manualmente a los founding accounts via seed o panel interno.

### 9.4 Frontend

- `JwtInterceptor` añade `Authorization` automáticamente.
- En 401 con `code == 'TOKEN_EXPIRED'`: intentar refresh, reintentar request original, si refresh falla → logout y redirect a `/login`.
- `AuthService` expone `currentUser = signal<User | null>(null)` y `isAuthenticated = computed(() => !!currentUser())`.
- `authGuard` redirige a `/login` si no hay sesión.

### 9.5 Cumplimiento legal (habeas data + términos de uso)

Obligaciones derivadas de la legislación colombiana (Ley 1581/2012, Decreto 1074/2015, Ley 1480/2011). Los documentos legales (Política de Tratamiento de Datos a nivel empresa + Términos y Condiciones a nivel app) viven fuera del código y se enlazan; aquí van solo los requisitos que la **aplicación** debe implementar.

> ⚠️ Los textos legales requieren revisión de un abogado. La app NO debe asumir contenido legal; solo enlaza los documentos vigentes y registra su aceptación.

**1. Aceptación en el registro (consentimiento informado):**
- El formulario de registro incluye un checkbox **obligatorio** (no premarcado) con texto: "He leído y acepto los [Términos y Condiciones] y la [Política de Tratamiento de Datos]" (ambos como enlaces).
- El registro falla (422) si no se acepta. El backend valida la aceptación, no solo el frontend.
- Se persiste la aceptación con **versión del documento + timestamp** por usuario.

**2. Persistencia de la aceptación** — tabla `legal_acceptances`:
```
id uuid PK
user_id text FK
document_type text         -- 'terms' | 'privacy'
document_version text      -- ej. 'v1.0'
accepted_at timestamptz
ip_address inet            -- evidencia razonable del consentimiento
```
Cuando cambie una versión vigente de un documento, se solicita re-aceptación al siguiente login.

**3. Versionado de documentos:** los documentos legales tienen versión (`v1.0`, `v1.1`...). La versión vigente se configura en `appsettings` (`Legal:TermsVersion`, `Legal:PrivacyVersion`) y se muestra en la UI.

**4. Canal de derechos del titular:** correo de habeas data (`Legal:DataProtectionEmail`, ej. `protecciondedatos@tododeia.com`) visible en la Política y en la pantalla de Settings. Las solicitudes (consultas/reclamos) se atienden en los plazos legales (consultas 10 días hábiles; reclamos 15 días hábiles). En MVP basta el canal de correo; no se requiere flujo in-app dedicado.

**5. Supresión de cuenta y datos:** el usuario puede solicitar la eliminación de su cuenta y datos personales. En MVP puede ser vía el correo de habeas data; idealmente, botón "Eliminar cuenta" en Settings que dispare borrado/anonimización (respetando obligaciones legales de conservación, ej. facturación).

**6. Datos de terceros (rol de Encargado):** LitigApp procesa datos de clientes y contrapartes que el abogado carga. El abogado es Responsable; LitigApp es Encargado. Los Términos incluyen la cláusula donde el usuario garantiza tener autorización para tratar esos datos. No requiere desarrollo adicional, pero condiciona el texto legal.

**7. Dónde viven los documentos (hosting):**
- **MVP — rutas estáticas dentro de la app Angular:** `app.litigapp.co/legal/terminos` y `app.litigapp.co/legal/privacidad`. El contenido vive como **HTML/Markdown estático versionado en el repo del frontend** (ej. `src/assets/legal/terminos.v1.0.md`), renderizado en una pantalla simple. NO se enlaza a Google Docs en producción.
- **Flujo de fuente de verdad:** los Google Docs son solo la superficie de **redacción y revisión legal**. Una vez el abogado aprueba, el texto final se copia al repo como el contenido publicado. La versión del archivo (`v1.0`...) debe coincidir con `Legal:TermsVersion` / `Legal:PrivacyVersion`.
- **Por qué en repo:** auditoría (git prueba qué versión estaba viva al momento de cada aceptación, junto con `legal_acceptances.document_version`), branding propio y sin dependencia externa.
- **Post-MVP:** cuando exista el sitio de empresa (`tododeia.com`), mover la **Política de Privacidad** (nivel empresa) a ese dominio y que la app enlace hacia afuera; los **Términos** (nivel app) permanecen en la app.
- **Enlaces:** ambos documentos enlazados desde el checkbox del registro, el **footer** global y la pantalla de Settings (abrir en pestaña nueva).

---

## 10. Background Jobs & Notifications

### 10.1 Sync Engine (rediseñado para WAF)

**Tres principios rectores**:
1. **Notificaciones agregadas por usuario**, NUNCA por proceso individual.
2. **Solo 2 endpoints en el ciclo diario**: overview + actions. detail/sujetos se consultan SOLO en creación inicial.
3. **WAF resilience**: trickle controlado + detección 403 + cooldown + retry desde donde quedamos.

```
╔═══════════════════════════════════════════════════════════════════╗
║  OverviewSweepJob — RecurringJob                                  ║
║  Cron: cada Sweep.OverviewIntervalMinutes (default 15min)         ║
║  Cola: "overview_sweep"                                           ║
║                                                                   ║
║  1. SELECT value_timestamp FROM sync_state                        ║
║       WHERE key='waf_blocked_until'                               ║
║     Si > now → SKIP esta corrida (registrar en log).              ║
║                                                                   ║
║  2. Cargar procesos elegibles:                                    ║
║       SELECT * FROM processes                                     ║
║       WHERE is_active=true                                        ║
║         AND ( sync_phase = 'pending_overview'                     ║
║               OR (sync_phase='idle'                               ║
║                   AND last_synced_at <                            ║
║                       now - Sweep.MinimumHoursBetweenSyncs))      ║
║       ORDER BY last_sync_attempt_at ASC NULLS FIRST   ← fairness  ║
║       LIMIT Sweep.BatchSize                                       ║
║                                                                   ║
║  3. Para cada proceso del lote:                                   ║
║       a) Esperar Throttle.OverviewInterval (con jitter)           ║
║       b) UPDATE last_sync_attempt_at = now                        ║
║       c) Llamar IRamaJudicialClient.GetOverviewByFileNumberAsync  ║
║       d) Casos:                                                   ║
║          • Éxito Y fechaUltimaActuacion > process.LastCourtAt:    ║
║              sync_phase='pending_actions'                         ║
║          • Éxito Y sin cambios:                                   ║
║              sync_phase='idle', last_synced_at=now                ║
║          • NotFound (200 vacío o 404):                            ║
║              sync_phase='idle', sync_status='not_found'           ║
║          • WafBlocked (403):                                      ║
║              ★ UPDATE sync_state SET waf_blocked_until =          ║
║                now + Waf.CooldownMinutesOnBlock                   ║
║              ★ Procesos restantes del lote quedan con             ║
║                sync_phase='pending_overview' (no se tocan)        ║
║              ★ BREAK del loop. Terminar el job aquí.              ║
║          • Transient (5xx tras Polly): sync_status='error',       ║
║              sync_attempts++, sync_phase NO cambia.               ║
║                                                                   ║
║  4. Al terminar el lote (con o sin 403):                          ║
║       Si hay procesos con sync_phase='pending_actions':           ║
║         BackgroundJob.Enqueue<ActionsSweepJob>(j => j.Run())      ║
║                                                                   ║
║  5. Adaptive throttle:                                            ║
║       Si esta corrida tuvo 0 bloqueos Y               procesados  ║
║         >= Waf.ConsecutiveSuccessesToSpeedUp:                     ║
║           current_overview_throttle = max(throttle-1, 2)          ║
║       Si esta corrida tuvo >= 1 bloqueo:                          ║
║           current_overview_throttle = min(throttle+2,             ║
║                                       Waf.EmergencyMaxThrottle)   ║
╚═══════════════════════════════════════════════════════════════════╝
                              │
                              ▼
╔═══════════════════════════════════════════════════════════════════╗
║  ActionsSweepJob — Encolado (NO recurrente)                       ║
║  Cola: "actions_sweep"                                            ║
║  Disparado al final de OverviewSweepJob si hay pending_actions    ║
║                                                                   ║
║  1. Si waf_blocked_until > now → reencolar para now + cooldown.   ║
║                                                                   ║
║  2. Cargar procesos con sync_phase='pending_actions'              ║
║     (sin LIMIT — son los que el sweep marcó).                     ║
║                                                                   ║
║  3. changedUsers = Set<userId>                                    ║
║                                                                   ║
║  4. Para cada proceso:                                            ║
║       a) Esperar Throttle.ActionsInterval (con jitter)            ║
║       b) Llamar GetFirstPageActionsAsync(externalProcessId)       ║
║       c) Casos:                                                   ║
║          • Éxito: aplicar diff por consecutive_number,            ║
║                    grouping Auto+Fijación.                        ║
║                    Si insertó nuevas → attended=false,            ║
║                    changedUsers.Add(process.UserId)               ║
║                    sync_phase='idle', sync_status='ok'.           ║
║          • WafBlocked (403):                                      ║
║              ★ Set waf_blocked_until. BREAK.                      ║
║              Restantes quedan con sync_phase='pending_actions'.   ║
║          • Otros errores: sync_status='error', sync_attempts++    ║
║                                                                   ║
║  5. Al terminar el lote:                                          ║
║     Para cada userId en changedUsers:                             ║
║       BackgroundJob.Enqueue<DispatchUserNotificationsJob>(        ║
║           j => j.RunAsync(userId))                                ║
║     ← La notificación arranca de INMEDIATO, sin esperar cron.     ║
╚═══════════════════════════════════════════════════════════════════╝

╔═══════════════════════════════════════════════════════════════════╗
║  CompletePartialFetchJob(processId) — Encolado                    ║
║  Cola: "partial_fetch"                                            ║
║  Para procesos creados via POST individual que quedaron parciales ║
║  (algún endpoint detail/subjects/actions falló por WAF o transient║
║   durante la creación síncrona).                                  ║
║                                                                   ║
║  1. Si waf_blocked_until > now → reencolar.                       ║
║  2. Reintentar SOLO los endpoints faltantes según sync_phase.     ║
║  3. Si éxito completo → sync_phase='idle', sync_status='ok'.      ║
║  4. Si 403 → set waf_blocked_until, dejarlo en partial.           ║
║  5. NO dispara notificación (el abogado ya creó el proceso).      ║
╚═══════════════════════════════════════════════════════════════════╝

╔═══════════════════════════════════════════════════════════════════╗
║  BulkImportJob(importJobId) — Encolado                            ║
║  Cola: "bulk_import" (1 worker — secuencial por usuario)          ║
║                                                                   ║
║  1. UPDATE import_jobs SET status='running'                       ║
║  2. Por cada fila del Excel (respeta waf_blocked_until):          ║
║       a) Validar/construir radicado 23 dígitos                    ║
║       b) Verificar unicidad (user_id, file_number)                ║
║       c) Esperar Throttle.InitialFetchInterval                    ║
║       d) Llamar overview → detail → sujetos → actions             ║
║          • Si 403 en cualquier punto:                             ║
║              persistir lo que tengamos, sync_phase=               ║
║              'pending_partial_completion', set waf_blocked_until, ║
║              encolar CompletePartialFetchJob(processId),          ║
║              ★ PAUSAR el bulk import: re-encolar el job a now +   ║
║                cooldown, marcar import_jobs.status='paused'.      ║
║              ★ Frontend muestra "Importación pausada por límite   ║
║                temporal de la API, reintentando en X minutos".    ║
║       e) UPDATE processed_rows / success_count / error_count      ║
║          cada 5 filas                                             ║
║  3. UPDATE import_jobs SET status='completed', completed_at=now   ║
║  4. Disparar email INMEDIATAMENTE (sin pasar por dispatcher):     ║
║       BackgroundJob.Enqueue<DispatchImportCompleteJob>(           ║
║           j => j.RunAsync(importJobId))                           ║
╚═══════════════════════════════════════════════════════════════════╝

╔═══════════════════════════════════════════════════════════════════╗
║  DispatchUserNotificationsJob(userId) — Encolado, triggered       ║
║  Cola: "notifications"                                            ║
║                                                                   ║
║  1. Cargar últimos N procesos del usuario con cambios desde       ║
║     última notificación enviada (por ahora: ventana de 24h).      ║
║  2. Si lista vacía → return (idempotente).                        ║
║  3. Construir digest payload con los procesos.                    ║
║  4. Cargar preferences. Si email_enabled:                         ║
║       - Insertar outbox (user_id, 'UserProcessesUpdated', 'email',║
║         payload) status='processing'.                             ║
║       - Llamar ResendEmailSender INMEDIATAMENTE.                  ║
║       - Si OK: outbox.status='sent', insertar notification_log.   ║
║       - Si error: outbox.status='pending' (queda para fallback).  ║
║  5. WhatsApp: SKIP en MVP (interface existe, no se envía).        ║
╚═══════════════════════════════════════════════════════════════════╝

╔═══════════════════════════════════════════════════════════════════╗
║  DispatchImportCompleteJob(importJobId) — Encolado, triggered     ║
║  Cola: "notifications"                                            ║
║                                                                   ║
║  Idéntico patrón: arma payload, llama Resend, marca log.          ║
║  NO pasa por outbox — es one-shot.                                ║
╚═══════════════════════════════════════════════════════════════════╝

╔═══════════════════════════════════════════════════════════════════╗
║  NotificationFallbackSweepJob — RecurringJob                      ║
║  Cron: "0 * * * *" (cada HORA, antes era cada 5 min)              ║
║  Cola: "notifications"                                            ║
║                                                                   ║
║  Solo se ejecuta para RECOGER HUÉRFANOS:                          ║
║    notifications_outbox WHERE status='pending'                    ║
║      AND created_at < now - 10 min                                ║
║      AND attempts < 5                                             ║
║  Los reintenta. Si attempts >= 5 → status='failed'.               ║
║                                                                   ║
║  Cadencia mucho menor = mucho menos bloat en Hangfire.            ║
╚═══════════════════════════════════════════════════════════════════╝

╔═══════════════════════════════════════════════════════════════════╗
║  OutboxCleanupJob — RecurringJob semanal                          ║
║  Borra outbox 'sent' o 'failed' con más de 30 días.               ║
║                                                                   ║
║  ImportJobsCleanupJob — RecurringJob semanal                      ║
║  Borra import_jobs con más de 90 días.                            ║
║                                                                   ║
║  HangfireRetentionPolicy:                                         ║
║  En Program.cs: JobExpirationTimeoutHours = 24                    ║
║  (default es 24h, lo dejamos explícito para evitar bloat).        ║
╚═══════════════════════════════════════════════════════════════════╝
```

**Resumen de cuándo se usa Hangfire y cuándo NO**:

| Acción | ¿Hangfire? | Razón |
|---|---|---|
| Sync diaria de procesos (overview) | Sí (`OverviewSweepJob`) | Recurrente cada 15min, configurable |
| Sync de actuaciones tras detectar cambio | Sí (`ActionsSweepJob`) | Triggered al final de OverviewSweep |
| Despacho de notificaciones | **Triggered** (`DispatchUserNotificationsJob`) | Sin polling agresivo; el fallback es cada 1h |
| Importación masiva por Excel | Sí (`BulkImportJob`) | 100+ procesos × tiempo de API = no sync |
| Creación individual con 23 dígitos | **NO** | UX = ver el modal con info al instante (4 calls × 1-3s = 4-12s) |
| Creación individual por wizard | **NO** | Idem |
| Fallback creación parcial | Sí (`CompletePartialFetchJob`) | Solo si algún call falló en la creación síncrona |
| Cleanup de outbox/imports viejos | Sí | Mantenimiento periódico |

### 10.2 Comportamiento ante WAF — resumen ejecutivo

| Evento | Acción |
|---|---|
| Primer 403 en una corrida | Set `waf_blocked_until = now + 20min`, abortar corrida actual |
| Próximo cron del sweep dentro del cooldown | Skip (log "esperando cooldown WAF") |
| Cron del sweep después del cooldown | Procesa procesos con `sync_phase='pending_*'` desde donde quedó |
| Bulk import bloqueado a mitad | Re-encolar a now+cooldown. Import job queda en `status='paused'`. Frontend lo refleja |
| Creación individual síncrona y 403 | Guardar lo obtenido (puede ser nada), `sync_phase='pending_partial_completion'`, encolar `CompletePartialFetchJob`. Devolver al usuario: "Tu proceso se registró. Cargaremos los detalles automáticamente en unos minutos." |
| 3 corridas consecutivas sin 403 | Bajar throttle de 3s a 2s (más velocidad) |
| 1 corrida con 403 | Subir throttle a 5s (más conservador) |

### 10.2 Ventana escalonada de sync

Para evitar enviar 10.000 requests al mismo segundo a la API:

```csharp
public async Task ExecuteAsync()
{
    var users = await _userRepo.GetAllAsync();
    foreach (var user in users)
    {
        // hash → 0..179 minutos desde 06:00
        var offsetMin = Math.Abs(user.Id.GetHashCode()) % 180;
        var scheduledAt = TodayAt(06, 00).AddMinutes(offsetMin);
        BackgroundJob.Schedule<SyncUserProcessesJob>(j => j.ExecuteAsync(user.Id), scheduledAt);
    }
}
```

### 10.3 Despacho de notificaciones (triggered + agregado por usuario)

**Cambio clave**: NO emitimos un evento por proceso. Emitimos UN trigger por usuario al detectar cambios en SU ciclo de sync (sea overview/actions sweep o bulk import).

`DispatchUserNotificationsJob` se encola al final de `ActionsSweepJob` para cada usuario que tuvo cambios:

```csharp
public async Task RunAsync(string userId, CancellationToken ct)
{
    // Idempotencia: cargar procesos del usuario con cambios recientes
    // (en últimas 24h desde la última notificación enviada)
    var lastNotifiedAt = await _logRepo.GetLastEmailSentAtAsync(userId, ct)
                        ?? DateTimeOffset.UtcNow.AddDays(-1);

    var changedSinceLast = await _processRepo.GetChangedSinceAsync(userId, lastNotifiedAt, ct);
    if (changedSinceLast.Count == 0) return;

    var user = await _userRepo.GetByIdAsync(userId, ct);
    var prefs = await _prefsRepo.GetAsync(userId, ct);

    var digest = BuildDigestPayload(changedSinceLast);

    if (prefs.EmailEnabled)
    {
        var outboxId = await _outboxRepo.InsertProcessingAsync(
            userId, "UserProcessesUpdated", "email", digest, ct);

        try
        {
            var result = await _emailSender.SendDigestAsync(user, digest, ct);
            await _outboxRepo.MarkSentAsync(outboxId, result.ProviderMessageId, ct);
            await _logRepo.InsertAsync(outboxId, userId, "email",
                changedSinceLast.Select(p => p.Id).ToArray(), "delivered", ct);
        }
        catch (Exception ex)
        {
            await _outboxRepo.MarkPendingForRetryAsync(outboxId, ex.Message, ct);
            // Fallback sweep lo recogerá en la próxima hora
        }
    }

    // WhatsApp: FUERA DEL MVP. La interfaz IWhatsAppSender existe
    // pero la implementación no se llama. Cuando se active en v2,
    // se agregará aquí el mismo patrón.

    await _unitOfWork.SaveChangesAsync(ct);
}
```

**Beneficios**:
- 1 abogado con 10 procesos cambiados → 1 email (no 10).
- **Despacho inmediato**: ya no hay polling cada 5 min. La notificación se envía en segundos tras detectar el cambio.
- **Outbox sigue siendo durable**: si Resend falla, queda `status='pending'` y el `NotificationFallbackSweepJob` (cada 1h) lo recoge.
- Idempotencia: si el job se reintenta, los UNIQUE constraints en `process_actions(external_action_id)` evitan duplicar actuaciones, y la query `GetChangedSinceAsync` desde `lastNotifiedAt` evita reenvíos duplicados.

### 10.3.1 Observabilidad y logging (cross-cutting concerns)

**Stack**: Serilog (logs estructurados) + OpenTelemetry (traces y métricas) + ASP.NET Core Health Checks. Ver dotnet-toolkit:serilog, dotnet-toolkit:opentelemetry y dotnet-toolkit:logging skills para patterns idiomáticos.

**Configuración base en `Program.cs`** (parte de la infraestructura inicial, propiedad de la vertical Cuenta + Infra):

```csharp
// Bootstrap Serilog antes de construir el host
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .CreateBootstrapLogger();

builder.Host.UseSerilog((ctx, services, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

// Request logging middleware
app.UseSerilogRequestLogging(opts =>
{
    opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} → {StatusCode} in {Elapsed:0.0000} ms";
    opts.GetLevel = (httpCtx, elapsed, ex) => ex != null
        ? LogEventLevel.Error
        : httpCtx.Response.StatusCode > 499
            ? LogEventLevel.Error
            : elapsed > 1000 ? LogEventLevel.Warning : LogEventLevel.Information;
});
```

**LoggingBehavior** (cross-cutting que envuelve TODOS los handlers CQRS):

```csharp
// LitigApp.Application/Common/Behaviors/LoggingBehavior.cs
public sealed class LoggingBehavior<TCommand, TResponse>(ILogger<LoggingBehavior<TCommand, TResponse>> logger)
    where TCommand : ICommand<TResponse>
{
    public async Task<Result<TResponse>> Handle(
        TCommand command,
        Func<Task<Result<TResponse>>> next,
        CancellationToken ct)
    {
        var requestName = typeof(TCommand).Name;
        var sw = Stopwatch.StartNew();
        logger.LogDebug("Handling {RequestName}", requestName);
        try
        {
            var result = await next();
            sw.Stop();
            if (result.IsSuccess)
                logger.LogInformation("Handled {RequestName} in {Elapsed}ms", requestName, sw.ElapsedMilliseconds);
            else
                logger.LogWarning("Handled {RequestName} with error {Error} in {Elapsed}ms",
                    requestName, result.Error, sw.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "Exception handling {RequestName} after {Elapsed}ms",
                requestName, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
```

Registrado vía Decorator en DI. Aplica a Commands automáticamente; los Queries lo pueden saltar (mucho ruido).

**Logging por capa**:

| Capa | Qué loggea | Nivel típico |
|---|---|---|
| Request middleware (Serilog) | HTTP method, path, status, duración | Information / Warning / Error |
| LoggingBehavior (Commands) | Handler in/out + duración + Result | Debug / Information / Warning |
| Handlers (Queries simples) | Nada de rutina | Debug solo en cache hit/miss |
| Handlers (Commands complejos) | Business events específicos | Information |
| External clients (Rama Judicial) | Request, response, retries, WAF 403, cooldowns | Information / Warning / Error |
| Jobs Hangfire | Inicio/fin, count procesados, errores | Information / Warning / Error |
| Infrastructure errors | Excepciones inesperadas | Error con stack trace |

**Anti-patterns prohibidos**:
- `_logger.LogInformation` en cada handler de Query rutinaria — se vuelve ruido.
- `string.Format` o interpolación en mensajes de log — usar template + parámetros estructurados.
- Loggear PII (passwords, tokens, datos personales completos del abogado).
- Log + throw del mismo error en niveles distintos (loggea solo donde manejas).

**Sinks**:
- Dev local: `Console` + opcionalmente `Seq` (`http://localhost:5341` en docker-compose).
- Producción: `Console` (Railway lo captura) + `BetterStack` o `Logtail` con structured fields.

**OpenTelemetry** (Tier 1+):
- ActivitySource custom para sync engine y outbox dispatcher.
- Metrics: `litigapp.sync.processes_synced`, `litigapp.sync.waf_blocks`, `litigapp.notifications.sent`, `litigapp.api.rama_judicial.duration`.
- Export OTLP cuando lleguemos a Tier 1 (Aspire Dashboard local, BetterStack o Honeycomb en prod).

### 10.4 Templates de notificación (formato DIGEST — solo EMAIL en MVP)

**Email digest (Resend) — `UserDigestEmailTemplate.cs`**:

```
Asunto: Tienes {N} novedades en tus procesos — LitigApp

Hola {fullName},

Detectamos cambios en {N} de tus procesos hoy:

┌────────────────────────────────────────────────────────────────────┐
│ Radicado              │ Nuevo estado         │ Fecha       │ Juzgado│
├────────────────────────────────────────────────────────────────────┤
│ 17001400301020240019200│ Fijacion estado     │ 2026-03-20  │ ...   │
│ 66001233100020120021100│ Auto admite demanda │ 2026-03-19  │ ...   │
│ ...                                                                 │
└────────────────────────────────────────────────────────────────────┘

[ Ver novedades en LitigApp ] → https://app.litigapp.co/novelties

Recibes este correo porque tienes activadas las notificaciones por email.
Puedes ajustar tus preferencias en https://app.litigapp.co/settings

— Equipo LitigApp
```

Renderizado con tabla HTML real (responsive). Si hay 1 solo proceso cambiado, el asunto y cuerpo se ajustan a singular ("Tienes 1 novedad...").

**WhatsApp digest — FUERA DEL MVP**

La interfaz `IWhatsAppSender` queda implementada como **stub no-op** en MVP:

```csharp
// LitigApp.Infrastructure/Notifications/WhatsApp/NoOpWhatsAppSender.cs
public sealed class NoOpWhatsAppSender : IWhatsAppSender
{
    private readonly ILogger<NoOpWhatsAppSender> _logger;
    public NoOpWhatsAppSender(ILogger<NoOpWhatsAppSender> logger) => _logger = logger;
    public Task<Result<WhatsAppSendResult>> SendDigestAsync(/*...*/)
    {
        _logger.LogInformation("WhatsApp send skipped (out of MVP scope)");
        return Task.FromResult(Result<WhatsAppSendResult>.Success(WhatsAppSendResult.Skipped));
    }
}
```

Cuando llegue v2:
1. Registrar templates en Meta Business Manager.
2. Implementar `MetaCloudWhatsAppSender` reemplazando el stub.
3. Habilitar `whatsapp_enabled` por defecto en `user_notification_preferences`.

**Plantilla planificada para v2** (`user_digest_v1`):
```
Hola {{1}}, tienes {{2}} novedades en tus procesos:
{{3}}
Abre LitigApp: https://app.litigapp.co/novelties
```
Donde `{{3}}` enumera hasta 5 procesos (límite ~1024 chars).

**Email de import complete — `ImportCompleteEmailTemplate.cs`**:

```
Asunto: Importación completada — {successCount} procesos cargados

Hola {fullName},

Terminamos de procesar tu archivo {fileName}.

✅ Cargados correctamente: {successCount}
⚠️  Con errores: {errorCount}
📊 Total procesado: {totalRows}

{si errorCount > 0:}
Errores detectados:
- Fila 5: Radicado no válido
- Fila 12: Despacho no encontrado en catálogo
...

[ Ver mis procesos ] → https://app.litigapp.co/processes

— Equipo LitigApp
```

**No se envía WhatsApp para ImportComplete** — UX: el abogado está en la app activamente durante la importación, ve el progreso. Email es suficiente como cierre.

Las plantillas WhatsApp deben ser **registradas y aprobadas en Meta Business Manager** antes de usar. El blueprint asume nombres `user_digest_v1`.

---

## 11. Build Order (Orden de Construcción Paso a Paso)

> Cada paso es independiente y verificable. El agente debe confirmar que cada paso compila/corre antes de pasar al siguiente.

### Step 0: Spike de validación de la API Rama Judicial (1 día)

**Objetivo**: confirmar que los DTOs propuestos en la sección 6.2 mapean correctamente las respuestas reales y que no hay sorpresas.

1. Crear un proyecto consola temporal `ApiSpike.csproj`.
2. Hacer las 4 llamadas (overview, detalle, sujetos, actuaciones) con un radicado conocido y otro inválido.
3. Validar que los DTOs deserializan sin error.
4. Verificar el cálculo de `LastExternalConsecutive`.
5. Confirmar la discrepancia `idProceso` (9 dígitos) vs `idRegProceso` (8 dígitos en ejemplo del usuario): determinar si es typo o real.
6. Documentar quirks reales: ¿bloquea por IP? ¿qué errores devuelve con caracteres especiales?

**Output**: README de hallazgos en `docs/api-spike-findings.md`. Ajustar DTOs si fuera necesario.

### Step 1: Scaffolding del backend

```bash
# Crear solución y proyectos
# NOTA: --format sln fuerza el formato clásico .sln en lugar del nuevo .slnx
# (default en .NET 10 SDK). .slnx no es reconocido por VS 2022 < 17.13.
# Mantenemos .sln clásico para compatibilidad con el equipo.
dotnet new sln -n LitigApp --format sln
dotnet new classlib -n LitigApp.Domain -o src/LitigApp.Domain
dotnet new classlib -n LitigApp.Application -o src/LitigApp.Application
dotnet new classlib -n LitigApp.Infrastructure -o src/LitigApp.Infrastructure
dotnet new classlib -n LitigApp.Jobs -o src/LitigApp.Jobs
dotnet new webapi -n LitigApp.Api -o src/LitigApp.Api --use-controllers
dotnet new xunit -n LitigApp.Domain.UnitTests -o tests/LitigApp.Domain.UnitTests
dotnet new xunit -n LitigApp.Application.UnitTests -o tests/LitigApp.Application.UnitTests
dotnet new xunit -n LitigApp.Api.IntegrationTests -o tests/LitigApp.Api.IntegrationTests

# Agregar a solución
dotnet sln add src/**/*.csproj tests/**/*.csproj

# Establecer referencias
dotnet add src/LitigApp.Application reference src/LitigApp.Domain
dotnet add src/LitigApp.Infrastructure reference src/LitigApp.Application src/LitigApp.Domain
dotnet add src/LitigApp.Jobs reference src/LitigApp.Application src/LitigApp.Infrastructure src/LitigApp.Domain
dotnet add src/LitigApp.Api reference src/LitigApp.Application src/LitigApp.Infrastructure src/LitigApp.Jobs src/LitigApp.Domain
```

Crear `Directory.Build.props` con:
```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <LangVersion>14</LangVersion>
  </PropertyGroup>
</Project>
```

`docker-compose.yml` con Postgres 15 local. Confirmar que `docker compose up -d` levanta Postgres en localhost:5432.

**Verificación**: `dotnet build` pasa sin errores.

### Step 2: Setup de Postgres + EF Core + primera migración

1. Instalar NuGets en `Infrastructure`:
   - `Npgsql.EntityFrameworkCore.PostgreSQL`
   - `Microsoft.EntityFrameworkCore.Design` (también en `Api`)
   - `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
2. Crear `AppDbContext` con `DbSet<Process>`, `DbSet<Court>`, etc. y `IdentityDbContext<ApplicationUser>` heredado.
3. Configurar connection string en `appsettings.Development.json`: `"Postgres": "Host=localhost;Database=litigapp;Username=postgres;Password=postgres"`.
4. `dotnet ef migrations add Initial -p src/LitigApp.Infrastructure -s src/LitigApp.Api -o Persistence/Migrations`.
5. `dotnet ef database update -p src/LitigApp.Infrastructure -s src/LitigApp.Api`.

**Verificación**: tablas creadas en Postgres local, incluidas `AspNetUsers` y todas las del dominio.

### Step 3: Seed del catálogo geográfico Colombia

1. Encontrar y subir al repo (`data/` folder) un dataset de departamentos y municipios DANE en JSON/CSV (públicos).
2. Crear `CatalogSeeder` en `Infrastructure/Persistence/Seeders/` que lee el dataset y hace bulk insert.
3. Crear seeders de `entities` y `specialties` con los códigos oficiales Rama Judicial conocidos (ej: 03 = CIVIL, 04 = PENAL, etc.).
4. Para `courts`: agregar comando `dotnet run --project src/LitigApp.Api -- seed-courts` que itera por las llamadas API y carga el catálogo nacional (job de una sola vez, NO automático).
5. Ejecutar al menos para 1 o 2 departamentos en dev.

**Verificación**: queries simples como `SELECT * FROM departments LIMIT 5;` retornan datos.

### Step 4: Auth con ASP.NET Identity + JWT

1. NuGets: `Microsoft.AspNetCore.Authentication.JwtBearer`.
2. Configurar Identity en `Program.cs`:
   ```csharp
   builder.Services.AddIdentity<ApplicationUser, IdentityRole>(opts => {
       opts.Password.RequiredLength = 8;
       opts.User.RequireUniqueEmail = true;
       opts.SignIn.RequireConfirmedEmail = false; // true en prod
   })
   .AddEntityFrameworkStores<AppDbContext>()
   .AddDefaultTokenProviders();
   ```
3. Implementar `JwtTokenService` con `JwtSecurityTokenHandler`.
4. `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)`.
5. Crear `AuthController` con endpoints register/login/refresh.
6. Crear `RegisterCommand`, `LoginCommand` con handlers en `Application/Features/Auth/`.

**Verificación**: registrar un usuario y hacer login retorna JWT válido (probar en Swagger).

### Step 5: Hangfire setup

1. NuGets: `Hangfire.AspNetCore`, `Hangfire.PostgreSql`.
2. Configurar en `Program.cs`:
   ```csharp
   services.AddHangfire(cfg => cfg.UsePostgreSqlStorage(connectionString));
   services.AddHangfireServer(opts => {
       opts.Queues = new[] { "notifications", "deep_sync", "initial_fetch", "default" };
       opts.WorkerCount = Environment.ProcessorCount * 2;
   });
   app.UseHangfireDashboard("/hangfire", new DashboardOptions {
       Authorization = new[] { new HangfireDashboardAuthFilter() }
   });
   ```
3. Crear `HangfireConfiguration.RegisterRecurringJobs()` con todos los recurrentes.

**Verificación**: dashboard `/hangfire` accesible (con auth Admin).

### Step 6: IRamaJudicialClient + Polly + DTOs

1. NuGets en `Infrastructure`: `Microsoft.Extensions.Http.Polly`, `Polly`.
2. Crear los DTOs en `Infrastructure/ExternalApis/RamaJudicial/Dtos/`.
3. Implementar `RamaJudicialClient` con `HttpClient` inyectado.
4. Registrar en DI:
   ```csharp
   services.AddHttpClient<IRamaJudicialClient, RamaJudicialClient>(client => {
       client.BaseAddress = new Uri(options.BaseUrl);
       client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
       client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
   })
   .AddPolicyHandler((sp, _) => PollyPolicies.BuildResiliencePolicy(options, sp.GetRequiredService<ILogger<RamaJudicialClient>>()));
   ```
5. Manejar casos especiales (200 vacío, 404, 500) en el cliente, no en el handler.

**Verificación**: test integración llama a un radicado real y deserializa OK.

### Step 7: CRUD de procesos — creación con full number (SÍNCRONO)

1. Crear handlers en `Application/Features/Processes/Commands/CreateProcessByFullNumber/`.
2. Validator: 23 dígitos, todos numéricos.
3. Handler **síncrono** (sin Hangfire):
   - Verifica que NO haya import activo del usuario (→ 409 si lo hay).
   - Verifica unicidad.
   - Llama secuencialmente a `IRamaJudicialClient`: overview → detail → subjects → actions.
   - Si overview retorna null → falla con `PROCESS_NOT_FOUND_IN_RAMA`.
   - Persiste todo en una sola transacción.
   - Si algún call DESPUÉS de overview falla tras Polly → guarda lo disponible con `sync_status='partial'` y encola `CompletePartialFetchJob`.
4. Crear `ProcessesController` con endpoint POST.
5. Implementar `CompletePartialFetchJob` en `Jobs/ProcessSyncJobs/` (solo se invoca en fallback).

**Verificación**: POST con un radicado real retorna 201 con sujetos y actuaciones en < 15s. Probar también con radicado inexistente (debe dar 422).

### Step 8: Wizard de creación con partes + catálogos (también SÍNCRONO)

1. Endpoints catálogo en `CatalogController`.
2. Handler `CreateProcessByWizardCommand`: invoca `FileNumber.Compose(...)` y luego MISMO flujo que `CreateProcessByFullNumber` (idealmente compartido vía un servicio de dominio `ProcessProvisioningService`).
3. Tests unit del value object con casos edge (consecutivo con menos de 7 dígitos, padding con ceros a la derecha).

### Step 9: Importación de Excel (mapeo de columnas) + bloqueo de UI

**Límites duros anti-OOM** (configurar en `appsettings.json`):
```json
"Import": {
  "MaxFileSizeBytes": 2097152,        // 2 MB
  "MaxRows": 5000,                    // máximo 5000 procesos por import
  "PreviewCacheTtlMinutes": 10
}
```

- **Validación en el controller** ANTES de leer con ClosedXML:
  - `[RequestSizeLimit(2 * 1024 * 1024)]` en el endpoint multipart.
  - Si excede tamaño → 413 Payload Too Large.
- **Validación tras parse**: si `totalRows > MaxRows` → 422 con ProblemDetails (`code='TOO_MANY_ROWS'`).
- Rationale: ClosedXML carga el DOM completo en memoria. 2MB + 5000 filas cubre el 99.99% de portafolios reales de un abogado individual.
- **Si en producción real vemos OOMs**: migrar a `ExcelDataReader` (streaming, más eficiente, API menos amigable). Documentado pero NO se hace en MVP.

**Endpoints backend**:

1. NuGet `ClosedXML`.
2. `ClosedXmlExcelParser` lee `.xlsx` y retorna `PreviewResult { columns, rows }`.
3. `POST /imports/preview` recibe multipart (límite 2MB enforced), parsea, valida row count, cachea preview en memoria con TTL 10 min (clave: `previewId` random).
4. `POST /imports` con `previewId + mapping`:
   - Verifica que NO haya import activo del usuario (un import a la vez por usuario).
   - Crea `ImportJob` con `status='pending'`.
   - Encola `BulkImportJob(importJobId)` en cola `bulk_import`.
   - Devuelve 202 Accepted con `{ importJobId, status: 'pending' }`.
5. **`GET /imports/active`** — endpoint **único** que el frontend usa para todo el ciclo del import. Contrato canónico (ver §5): **`{ hasActive: boolean, importJob: {...} | null }`**, sin envelope.
   - Si hay job en curso (`pending`/`running`): `hasActive=true` e `importJob` con campos `{ id, fileName, totalRows, processedRows, successCount, errorCount, status, errors, createdAt, completedAt }`.
   - Si el último job terminó hace **menos de 60 segundos**: también `hasActive=true` con `importJob.status='completed'` (ventana corta para que el frontend detecte la finalización vía polling y dispare popup + refresh).
   - Pasados esos 60s, o si nunca hubo import: **`{ hasActive: false, importJob: null }`**.
6. `GET /imports/{id}` — opcional, solo para consulta directa de un job específico (link desde email, historial). No se usa en el flujo principal.
7. `BulkImportJob`:
   - `UPDATE import_jobs SET status='running'` al inicio.
   - Por cada fila, aplica la **estrategia de importación v1** (ver abajo).
   - Las filas importadas quedan `attended=true` (es importación, no novedad).
   - **Actualiza `processed_rows`, `success_count`, `error_count` cada 5 filas** para que el polling tenga datos frescos.
   - Al terminar: `status='completed'`, `completed_at=now`, inserta outbox con `event_type='ImportComplete', channel='email'`.

**Estrategia de importación v1 — "radicado-first", best-effort** (validada contra un Excel real de un abogado en ejercicio):

- **Mapeo de columnas dirigido por el usuario.** Cada abogado nombra y ordena sus columnas distinto (incluso puede no ser Excel), así que el sistema NO hardcodea posiciones. El usuario mapea:
  - **Obligatorio:** la columna del **radicado**.
  - **Opcional:** la columna de **notas/observaciones** (se guarda como nota del proceso).
- **Por cada fila:**
  1. Lee la columna de radicado y **normaliza**: quita espacios, guiones, tabs y todo no-dígito.
  2. ¿Quedan **exactamente 23 dígitos**?
     - **Sí** → importar vía flujo full-number (4 endpoints; la API + los primeros 5 dígitos DANE llenan depto/municipio/despacho). Se ignora cualquier otra columna de ubicación.
     - **No** (vacío, SIC tipo `25-463759-0`, penal/Fiscalía de 21 díg., basura) → **NO se intenta reconstruir**. La fila va a la **lista de errores** con `error.code='INVALID_RADICADO'` para carga manual con el wizard.
  3. **Filas vacías y filas de sección/encabezado** (sin radicado en la columna mapeada) → **se saltan en silencio**; NO cuentan como error.
- **`column_mapping` es extensible por diseño:** el shape v1 es `{ radicadoCol, notesCol }`, pero la estructura admite a futuro `{ yearCol, sequenceCol, deptCol, cityCol, courtCol, ... }` sin romper nada. El frontend solo expone radicado (obligatorio) + notas (opcional) en v1.
- **Importación "por partes" → v2 (no v1):** la idea de mapear año/consecutivo/depto/municipio/juzgado y reconstruir el radicado es válida, pero el cuello de botella NO es año/consecutivo (esos son fáciles) sino resolver el **`official_code` (12 díg.) del despacho**: el Excel del abogado trae un *nombre* de juzgado, no su código, y depto+municipio no identifican un juzgado único. Eso obliga a **matching difuso del nombre del juzgado** contra el catálogo `courts`, cuyo riesgo es importar el **proceso equivocado en silencio** (radicado válido del despacho incorrecto) — inaceptable en una herramienta legal. Por eso se difiere a v2 y, cuando se haga, será con **match exacto normalizado + paso de confirmación humana** antes de importar, nunca fuzzy a ciegas. En v1, toda fila sin radicado de 23 díg. válido → carga manual con el wizard.
- **Formato/encoding:** soportar **`.xlsx` vía ClosedXML** (interno UTF-8, sin romper acentos). `.csv` (Windows-1252, rompe `Villamar�a`) fuera de MVP.
- **Fuera de la Rama Judicial:** procesos de la SIC y penales (Fiscalía) no están en la API; caen naturalmente a manual por no tener radicado de 23 díg. La app no los monitorea automáticamente en v1.

**Flujo en el frontend** (importante):

```
1. Usuario sube Excel → mapea columnas → confirma → POST /imports
2. Backend devuelve { importJobId, status: "pending" }
3. Frontend cierra el modal de carga y muestra un BANNER GLOBAL FIJO
   en la parte superior con: "📥 Importando procesos... (12/87)"
   + barra de progreso.
4. Mientras el banner esté visible:
   - El botón "Agregar Proceso" queda DESHABILITADO en cualquier pestaña,
     con tooltip "Espera a que termine la importación".
   - Frontend hace polling a GET /imports/active CADA 3 SEGUNDOS.
5. Cada respuesta del polling actualiza el banner con el nuevo progreso
   (processed_rows / total_rows).
6. Cuando el polling devuelve status='completed':
   a. DETENER el polling.
   b. RE-HABILITAR "Agregar Proceso".
   c. Mostrar popup/modal de resumen:
        "✅ Importación completada
         · 82 procesos cargados
         · 5 procesos con errores  [Ver detalles]"
      (si error_count > 0, botón para descargar CSV de errores)
   d. AL CERRAR el popup (o hacer clic en "Ver mis procesos"):
        - REDIRIGIR automáticamente a la pestaña /processes.
        - FORZAR RELOAD del listado para que los nuevos aparezcan.
   e. OCULTAR el banner global.
7. En paralelo, el email "import complete" ya llegó al abogado
   (notificación extra-app, sirve si cerró el navegador).
```

**Implementación del banner** (Angular): servicio singleton `ImportProgressService` con un signal `activeImport = signal<ImportJobStatus | null>(null)`.

**Polling — reglas estrictas (NO ocioso):**
- El polling a `GET /imports/active` **solo arranca cuando el usuario inicia un import** (tras `POST /imports` exitoso, usando el `importJobId` devuelto).
- Se **auto-detiene** apenas la respuesta trae `status='completed'` o `'failed'` (o ante error de red) — hacer `clearInterval`/completar el stream ahí mismo. Jamás un `setInterval` perpetuo atado solo a `ngOnDestroy`.
- En estado idle (sin import en curso) el dashboard **NO llama** `/imports/active`. **Cero polling de fondo.** El caso "el usuario cerró la app durante el import" lo cubre el **email** de finalización, no un polling permanente.
- La lógica de polling vive en el `ImportProgressService`, **no** en `dashboard.component` (que solo lee el signal). El componente `<global-import-banner>` se renderiza condicionalmente en `dashboard.component.html` (bajo el header) leyendo ese signal.

**Bloqueo en el backend (defensa en profundidad)**: aunque el frontend deshabilita el botón, los endpoints `POST /processes/full-number` y `/wizard` también validan que no haya import activo del usuario → devuelven 409 con `error.code='IMPORT_IN_PROGRESS'` si alguien intenta llamar la API directamente (curl, Postman, segunda pestaña abierta). Esto evita race conditions y mal uso.

**Verificación**: subir Excel con 10 procesos. Banner aparece y se actualiza cada 3s. POST a `/processes/full-number` desde Postman devuelve 409. Al terminar: popup sale, banner desaparece, redirige a /processes, los procesos están en la lista. Email llega en paralelo.

### Step 10: Sync engine WAF-resilient (OverviewSweep + ActionsSweep)

1. **Crear tabla `sync_state`** + seed inicial (`waf_blocked_until=null`, throttle defaults).
2. **Crear `SyncStateService`** en Infrastructure con métodos:
   - `Task<DateTimeOffset?> GetWafBlockedUntilAsync()`
   - `Task SetWafBlockedUntilAsync(DateTimeOffset until, string reason)`
   - `Task<int> GetOverviewThrottleSecondsAsync()` / `SetOverviewThrottleSecondsAsync(int)`
3. **Implementar `OverviewSweepJob`** como RecurringJob (cron configurable, default cada 15 min):
   - Check `waf_blocked_until` — si > now, log y return.
   - Cargar batch de procesos ordenados por `last_sync_attempt_at ASC NULLS FIRST`.
   - Loop: throttle + jitter, llamar `IRamaJudicialClient.GetOverviewByFileNumberAsync`.
   - On `FailureKind.WafBlocked`: set `waf_blocked_until = now + cooldown`, break loop.
   - On success: actualizar `sync_phase` (`pending_actions` si cambió, `idle` si no).
   - Al terminar: si hay `pending_actions` → `BackgroundJob.Enqueue<ActionsSweepJob>(...)`.
   - Adaptive throttle: ajustar `current_overview_throttle_seconds` según éxitos/bloqueos.
4. **Implementar `ActionsSweepJob`** (encolado, no recurrente):
   - Check `waf_blocked_until` — si > now, re-encolar a `now + cooldown`.
   - Cargar procesos con `sync_phase='pending_actions'` (sin limit, son los marcados).
   - Loop: throttle + jitter, llamar `GetFirstPageActionsAsync`.
   - Aplicar diff por `consecutive_number > last_external_consecutive`.
   - Aplicar grouping Auto+Fijación.
   - Acumular `changedUsers` set.
   - Al terminar: para cada userId → `BackgroundJob.Enqueue<DispatchUserNotificationsJob>(j => j.RunAsync(userId))`.
5. **Implementar `CompletePartialFetchJob`**: para procesos en `sync_phase='pending_partial_completion'`, reintentar solo los endpoints faltantes.
6. **Dedupe**: UNIQUE constraint en `process_actions(process_id, external_action_id)`. Manejar `DbUpdateException` con `unique_violation` silenciosamente.

**Verificación**: simular WAF bloqueando (mock que devuelve 403 al 5to call). Verificar que: el job se detiene, `sync_state` queda con `waf_blocked_until` en futuro, el próximo cron se salta, después del cooldown retoma desde el proceso #6.

### Step 11: Notificaciones email (Resend) — digest + triggered

1. Implementar `ResendEmailSender` con `HttpClient` a `https://api.resend.com/emails`.
2. Plantilla HTML `UserDigestEmailTemplate.cs` (tabla con procesos cambiados).
3. Plantilla HTML `ImportCompleteEmailTemplate.cs` (resumen del import).
4. Implementar `DispatchUserNotificationsJob` (triggered desde ActionsSweepJob).
5. Implementar `DispatchImportCompleteJob` (triggered desde BulkImportJob).
6. Implementar `NotificationFallbackSweepJob` cron HORARIO (no cada 5 min): recoge outbox huérfanos con `created_at < now - 10min`.
7. Configurar Hangfire retention en `Program.cs`:
   ```csharp
   services.AddHangfire(cfg => cfg.UsePostgreSqlStorage(/* connectionString */, new PostgreSqlStorageOptions {
       JobExpirationCheckInterval = TimeSpan.FromHours(1),
       InvisibilityTimeout = TimeSpan.FromMinutes(30)
   }));
   GlobalConfiguration.Configuration.JobExpirationTimeout = TimeSpan.FromHours(24);
   ```

### Step 12: WhatsApp — FUERA DEL MVP

**No se implementa en MVP.** Crear únicamente:

1. Interfaz `IWhatsAppSender` en `Application/Common/Abstractions/`.
2. Stub `NoOpWhatsAppSender : IWhatsAppSender` en `Infrastructure/Notifications/WhatsApp/` que loguea y retorna `Result.Success(Skipped)`.
3. Registrar el stub en DI.
4. Documentar en `docs/v2-whatsapp-integration.md` los pasos para activar:
   - Registrar `user_digest_v1` template en Meta Business Manager.
   - Crear `MetaCloudWhatsAppSender` implementando la interfaz.
   - Swap del registro en DI.
   - Habilitar `whatsapp_enabled` default true en migración nueva.
   - Agregar UI de toggle en settings frontend.

Esto deja la arquitectura preparada sin costo de tiempo ni dinero en MVP.

### Step 13: PDF con QuestPDF

1. NuGet `QuestPDF`. **Importante**: declarar uso Community en `Program.cs`:
   ```csharp
   QuestPDF.Settings.License = LicenseType.Community;
   ```
2. Implementar `ProcessReportDocument : IDocument` con header, metadata, sujetos, actuaciones.
3. Endpoint `GET /processes/{id}/pdf` que retorna `FileStreamResult` con `Content-Type: application/pdf`.

### Step 14: Scaffolding del frontend (Angular 20)

```bash
pnpm dlx @angular/cli@20 new litigapp-web --routing --style=css --ssr=false --strict --standalone
cd litigapp-web
pnpm add -D tailwindcss @tailwindcss/postcss   # Tailwind v4 — NO autoprefixer (v4 lo incluye)
pnpm add lucide-angular @capacitor/core @capacitor/cli @capacitor/ios @capacitor/android
ng add @angular/pwa
npx cap init litigapp co.litigapp.app --web-dir=dist/litigapp-web/browser
```

**Setup de Tailwind v4 (CSS-first — crítico, aquí es donde se rompe si se hace con sintaxis v3):**

1. `.postcssrc.json` en la raíz:
   ```json
   { "plugins": { "@tailwindcss/postcss": {} } }
   ```
2. `src/styles.css` — **única forma correcta en v4** (NO usar `@tailwind base/components/utilities`, eso es v3 y con `@tailwindcss/postcss` no genera NADA):
   ```css
   @import "tailwindcss";

   @theme {
     /* tokens del design system §8 — así se consumen como bg-primary-600, text-muted, etc. */
     --color-primary-700: #1d4ed8;
     --color-primary-600: #2563eb;
     --color-primary-500: #3b82f6;
     --color-background: #f8fafc;
     --color-surface:    #ffffff;
     --color-border:     #e2e8f0;
     --color-text:       #1e293b;
     --color-muted:      #64748b;
     --font-sans: "Inter", ui-sans-serif, system-ui, sans-serif;
   }
   ```
3. **NO existe `tailwind.config.js` en v4.** El content-scanning es automático (incluye `.ts` y `.html`); la config va en `@theme`.
4. Verificación obligatoria: tras el scaffolding, `pnpm start` y confirmar visualmente que una clase de Tailwind (ej. `bg-primary-600`) **renderiza color**. Si no pinta, Tailwind no está generando utilidades — NO avanzar hasta arreglarlo.

Configurar `eslint.config.js` con `no-restricted-imports` para boundaries de capas y la regla de estructura de componentes (ver abajo). Habilitar **zoneless change detection** (estable en Angular 20) en `app.config.ts`:
```typescript
import { provideZonelessChangeDetection } from '@angular/core';
providers: [provideZonelessChangeDetection(), ...]
```

**Estructura de componentes (obligatoria, enforced por ESLint):** cada componente usa **archivos separados** — `xxx.component.ts` + `xxx.component.html` (`templateUrl`) + `xxx.component.css` (`styleUrl`). **Prohibido `template:` o `styles:` inline.** Regla en `eslint.config.js`:
```js
'@angular-eslint/component-max-inline-declarations': ['error', { template: 0, styles: 0, animations: 0 }]
```

### Step 15: Layout base + auth + interceptor

1. Componentes `LoginComponent`, `RegisterComponent`.
2. `AuthService` con signals `currentUser`, `isAuthenticated`.
3. `JwtInterceptor` con manejo de refresh.
4. Guards `authGuard`, `guestGuard`.
5. `DashboardComponent` con `<header>` + `<router-outlet>`.

### Step 16: Dashboard — pestañas Novedades y Procesos

1. `NoveltiesTabComponent`: signal items, fetch, paginación.
2. `ProcessesTabComponent`: filtros reactive form, debounce 300ms, paginación.
3. `DetailDialogComponent`: modal con info + botón "Marcar atendido" + "Descargar PDF".
4. Mutación optimista en "Marcar atendido".

### Step 17: Flujos de creación + importación

1. `AddProcessDialogComponent` con 2 tabs (manual / excel).
2. `WizardComponent` con stepper: depto → ciudad → despacho → consecutivo.
3. `ImportUploadStepComponent` → `MappingStepComponent` → `ProgressTrackerComponent` (polling).

### Step 18: Capacitor — empaque mobile

1. `pnpm build` Angular.
2. `npx cap add ios` y `npx cap add android`.
3. `npx cap sync`.
4. `npx cap open ios` (requiere macOS + Xcode) y `npx cap open android` (Android Studio).
5. Configurar splash, icons, permisos en `capacitor.config.ts`.

### Step 19: Deploy

1. **Backend en Railway**:
   - Conectar repo, seleccionar carpeta `litigapp-backend`.
   - Variables de entorno (ver sección 12).
   - Dos servicios: `litigapp-api` (con `Dockerfile.api`) y `litigapp-worker` (con `Dockerfile.worker`).
2. **Postgres en Supabase**:
   - Crear proyecto.
   - Copiar connection string a Railway env vars.
   - Habilitar `pg_trgm` extension.
   - Ejecutar migraciones desde Railway: `dotnet ef database update`.
3. **Frontend en Vercel**:
   - Conectar repo, root directory `litigapp-web`.
   - Build command: `pnpm build`. Output: `dist/litigapp-web/browser`.
   - Env var `API_URL` apuntando al servicio Railway.
4. **DNS**:
   - `app.litigapp.co` → Vercel.
   - `api.litigapp.co` → Railway.

### Step 20: Hardening + observabilidad

1. Rate limiting con `AspNetCoreRateLimit`: 100 req/min por IP en `/auth/*`, 1000/min general.
2. Serilog → console + Logtail sink.
3. Sentry frontend + backend.
4. Health checks: `/health/live` y `/health/ready` (chequea Postgres).
5. Backups de Postgres: Supabase Pro hace daily backup automático.

---

## 12. Environment Setup

### Prerrequisitos

- .NET 10 SDK
- Node.js 22+ y pnpm
- Docker Desktop (opcional para Postgres local; alternativa: Postgres nativo)
- Cuenta en Supabase, Railway, Vercel, Resend, Meta Business
- (Mobile) macOS con Xcode 16+ para iOS, Android Studio 2024+ para Android

### Variables de entorno (backend)

| Variable | Descripción | Dónde obtener |
|---|---|---|
| `ConnectionStrings__Postgres` | Connection string Supabase/Postgres | Supabase Dashboard → Settings → Database |
| `Jwt__Secret` | Secret HS256 (mín 32 chars) | Generar: `openssl rand -hex 32` |
| `Jwt__Issuer` | Issuer URL | `https://api.litigapp.co` |
| `Jwt__Audience` | Audience | `https://app.litigapp.co` |
| `Jwt__AccessTokenMinutes` | Vida del access token | `15` |
| `Jwt__RefreshTokenDays` | Vida del refresh token | `7` |
| `RamaJudicial__BaseUrl` | Base URL API | `https://consultaprocesos.ramajudicial.gov.co:448` |
| `RamaJudicial__TimeoutSeconds` | Timeout HTTP por request | `15` |
| `RamaJudicial__Throttle__OverviewIntervalSecondsMin` | Sleep mín entre requests overview | `2` |
| `RamaJudicial__Throttle__OverviewIntervalSecondsMax` | Sleep máx entre requests overview | `3` |
| `RamaJudicial__Throttle__ActionsIntervalSecondsMin` | Sleep mín entre requests actions | `2` |
| `RamaJudicial__Throttle__ActionsIntervalSecondsMax` | Sleep máx entre requests actions | `3` |
| `RamaJudicial__Waf__CooldownMinutesOnBlock` | Espera tras detectar 403 | `20` |
| `RamaJudicial__Sweep__OverviewIntervalMinutes` | Cron del OverviewSweep | `15` |
| `RamaJudicial__Sweep__BatchSize` | Procesos por corrida | `500` |
| `RamaJudicial__Proxy__Enabled` | Activar BrightData (Tier 2+) | `false` |
| `Resend__ApiKey` | API key Resend | resend.com → API Keys |
| `Resend__FromEmail` | Email remitente verificado | `notifications@litigapp.co` |
| `Import__MaxFileSizeBytes` | Tamaño máx Excel | `2097152` (2 MB) |
| `Import__MaxRows` | Filas máx por import | `5000` |
| ~~`WhatsApp__*`~~ | **FUERA DE MVP** — no configurar | — |
| `Hangfire__DashboardPassword` | Password básica para `/hangfire` | Generar fuerte |
| `Sentry__Dsn` | DSN Sentry backend | sentry.io |
| `Logtail__SourceToken` | Token Logtail | betterstack.com |

### Comandos iniciales (después de clonar)

```bash
# Backend
cd litigapp-backend
docker compose up -d                                          # Postgres local
dotnet restore
dotnet ef database update -p src/LitigApp.Infrastructure -s src/LitigApp.Api
dotnet run --project src/LitigApp.Api -- seed-catalog
dotnet run --project src/LitigApp.Api                         # http://localhost:5000

# Frontend
cd ../litigapp-web
pnpm install
pnpm start                                                    # http://localhost:4200

# Mobile (después de build)
pnpm build
npx cap sync
npx cap open ios            # o android
```

---

## 13. Dependencias

### Backend — NuGets principales

| Paquete | Versión | Propósito |
|---|---|---|
| Microsoft.AspNetCore.App | 10 | Web API |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 10 | Identity |
| Microsoft.AspNetCore.Authentication.JwtBearer | 10 | JWT |
| Microsoft.EntityFrameworkCore | 10 | ORM |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10 | Provider Postgres |
| Hangfire.AspNetCore | latest compatible | Jobs |
| Hangfire.PostgreSql | latest compatible | Storage Hangfire |
| Polly | 8+ | Resiliencia HTTP |
| Microsoft.Extensions.Http.Resilience | 10 | Integración HttpClient + Polly v8 nativa |
| FluentValidation.AspNetCore | 11+ | Validación |
| Mapster | 7+ | Mapping |
| ClosedXML | 0.104+ | Excel |
| QuestPDF | 2024+ | PDF |
| Serilog.AspNetCore | 8+ | Logging |
| Serilog.Sinks.BetterStack | latest | Logtail/Better Stack |
| Sentry.AspNetCore | latest | Errores |
| Swashbuckle.AspNetCore | 6+ | Swagger |
| AspNetCoreRateLimit | latest | Rate limiting |

### Backend — NuGets tests

| Paquete | Propósito |
|---|---|
| xunit, xunit.runner.visualstudio | Test runner |
| FluentAssertions | Aserciones legibles |
| Testcontainers.PostgreSql | Postgres real en tests integración |
| Microsoft.AspNetCore.Mvc.Testing | WebApplicationFactory |
| NSubstitute | Mocking |

### Frontend — npm

| Paquete | Propósito |
|---|---|
| @angular/core, common, router, forms (20+) | Angular |
| @angular/pwa | Service worker + manifest |
| @capacitor/core, cli, ios, android (7+) | Mobile wrapper |
| tailwindcss v4 + @tailwindcss/postcss | Estilos |
| lucide-angular | Iconos |
| rxjs | Reactividad HTTP |

### Frontend — dev

| Paquete | Propósito |
|---|---|
| typescript | strict mode |
| eslint, @typescript-eslint/* | Lint |
| @playwright/test | E2E |
| vitest o jest | Unit tests |

---

## 14. Deployment Strategy

### Hosting

- **Frontend (Angular)**: Vercel. Auto-deploy desde `main`. Preview deploys por PR.
- **Backend (.NET API)**: Railway servicio `litigapp-api`. Dockerfile multistage que copia binarios publicados.
- **Workers (.NET Jobs)**: Railway servicio `litigapp-worker`. Mismo image base, diferente entrypoint que NO expone HTTP (solo corre Hangfire workers).
- **BD (Postgres)**: Supabase. Free hasta el Tier 1.

### CI/CD

- **GitHub Actions** para tests:
  - `.github/workflows/backend-ci.yml`: `dotnet test` en cada push.
  - `.github/workflows/frontend-ci.yml`: `pnpm lint && pnpm test` en cada push.
- **Deploy automático**: Vercel y Railway tienen integración nativa con GitHub. Merge a `main` → deploy a producción.
- **Migraciones**: Railway corre `dotnet ef database update` como release command antes de iniciar el servicio.

### Docker: cuándo se usa

**Docker NO es obligatorio para el flujo normal de desarrollo**, pero sí lo necesitamos para producción y opcionalmente para onboarding rápido de devs.

**Para desarrollo local (opcional)** — `docker-compose.yml` en la raíz del backend:

```yaml
services:
  postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
      POSTGRES_DB: litigapp
    ports: ["5432:5432"]
    volumes: [pg_data:/var/lib/postgresql/data]
  seq:                                   # opcional, dashboard de logs Serilog
    image: datalust/seq:latest
    environment: { ACCEPT_EULA: "Y" }
    ports: ["5341:80"]
volumes: { pg_data: }
```

- Con `docker compose up -d` un dev nuevo tiene Postgres + Seq listos en 30 segundos.
- Alternativa: instalar Postgres nativo. Ambas opciones funcionan.
- El backend en sí **se corre con `dotnet run`, NO dentro de Docker en dev** (mejor DX, hot reload, debugger).
- El frontend se corre con `pnpm start`, **NO** Docker.

**Para producción (obligatorio)** — Railway requiere container. Dos Dockerfiles:

`Dockerfile.api` — API HTTP que escucha en :8080:
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/LitigApp.Api/LitigApp.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "LitigApp.Api.dll"]
```

`Dockerfile.worker` — Worker Hangfire que NO expone HTTP:
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/LitigApp.Api/LitigApp.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=                              # vacío = no escucha HTTP
ENV LITIGAPP_ROLE=worker                          # leído en Program.cs para no registrar controllers
ENTRYPOINT ["dotnet", "LitigApp.Api.dll"]
```

En `Program.cs`, según `LITIGAPP_ROLE`:
- `api` (default): registra controllers + Hangfire client + Hangfire server (workers livianos para colas críticas tipo notifications).
- `worker`: registra Hangfire server con MÁS workers para colas pesadas (user_sync, deep_sync, bulk_import). No expone endpoints.

**No usamos Dockerfile para frontend** — Vercel lo buildea nativo desde el repo.

### Environments

| Env | Frontend | Backend | DB |
|---|---|---|---|
| Local | `localhost:4200` | `localhost:5000` | Docker Postgres |
| Staging | `staging.litigapp.co` (Vercel preview) | `api-staging.up.railway.app` | Supabase staging project |
| Production | `app.litigapp.co` | `api.litigapp.co` | Supabase prod project |

---

## 15. Testing Strategy

### Unit Tests

- **Domain**: tests por value object (`FileNumber`), por entity behavior (`Process.ApplyNewActions`).
- **Application**: tests de handlers con mocks de repositorios. Validar Result.Success/Failure paths.

### Integration Tests

- `Testcontainers.PostgreSql` levanta Postgres efímera en cada run.
- `WebApplicationFactory` con `IClassFixture` para reusar el container.
- Tests por endpoint crítico: auth, crear proceso, listar novedades, importar Excel.

### E2E Tests (Playwright)

Flujos críticos solamente:
1. Registro → login → ver dashboard vacío.
2. Crear proceso con 23 dígitos → ver en pestaña Procesos.
3. Marcar como atendido → desaparece de Novedades.
4. Importar Excel pequeño → ver progreso → ver procesos.

Correr en CI bajo headless. Estables, no flakeys.

---

## 16. Scaling Roadmap (Tiers)

### Tier 0 — MVP / Validación (0-10 abogados, ~1.000 procesos)
- **Stack**: el descrito. Costo ~$5-10/mes.
- **Acción**: no tocar nada.

### Tier 1 — Producción inicial (10-100 abogados, ~10K procesos)
- **Costo**: ~$45-60/mes.
- **Acciones**:
  - Separar `litigapp-api` y `litigapp-worker` en 2 servicios Railway.
  - Subir a Supabase Pro ($25/mo).
  - Habilitar Logtail/Better Stack ($10/mo).
  - Configurar ventana escalonada de sync (ya en código).
  - Activar backups daily de Supabase (incluido en Pro).

### Tier 2 — Escala media (100-500 abogados, ~50K procesos)
- **Costo**: ~$200-350/mes (incluye BrightData).
- **Acciones**:
  - **🎯 Activar BrightData Web Unlocker** ($3-5/GB, estimado $20-50/mes a 100-500 abogados): el `IRamaJudicialClient` se cambia a `FailoverRamaJudicialClient` que combina directo + proxy. Setting: `RamaJudicial__Proxy__Enabled=true`.
    - **Indicador de cuándo activar**: si en 3 días consecutivos la corrida diaria no logra completar todos los procesos por bloqueos WAF.
    - Alternativas: ScrapingBee, ScraperAPI, Smartproxy — la abstracción permite cambiar de proveedor sin reescribir nada.
  - Postgres con read replica (Supabase Team o Neon).
  - Redis caché en Upstash para catálogo y sesiones.
  - Migrar outbox a broker real (CloudAMQP RabbitMQ Free o Azure Service Bus). Solo cambiar `IMessagePublisher` impl.
  - Múltiples worker instances Railway con throttle reducido (1s en lugar de 2-3s) si BrightData está activo.
  - **🎯 Activar WhatsApp**: implementar `MetaCloudWhatsAppSender` (~$15-30/mes a 100 abogados). Ver `docs/v2-whatsapp-integration.md`.

### Tier 3 — Escala grande (500-2000+ abogados, 200K+ procesos)
- **Costo**: $500-1500/mes.
- **Acciones**:
  - Migración opcional a Azure (con créditos partner).
  - Particionado de `process_actions` por año.
  - Datadog/New Relic para observabilidad.
  - Considerar separación de bounded contexts (sync engine separado del API).

### Decisiones que habilitan este roadmap (ya en MVP)

1. `IMessagePublisher` abstraction → swap a broker real sin tocar handlers.
2. `IRamaJudicialClient` abstraction → fácil agregar proxy/IP pool.
3. Repositorios + Unit of Work → migración a read replica centralizada.
4. Hangfire `RecurringJob.AddOrUpdate` → cambiar schedules sin redeploy.
5. EF Core migrations versionadas → cambios de esquema trazables.
6. `AsNoTracking` por defecto en queries lectura.
7. Health checks → restart automático Railway.
8. Idempotencia en jobs (UNIQUE en `external_action_id`).
9. Graceful shutdown en workers.
10. Logs estructurados con Serilog → migrables a cualquier sink.

---

## 17. Skills a Usar Durante el Build

| Skill | Cuándo | Por qué |
|---|---|---|
| `/frontend-design` | Steps 15-17 (layouts, dashboard, modales) | Producir UI de calidad alineada al mockup |
| `/playwright-cli` | Step 15+ (E2E tests) | Automatización de tests críticos |
| `/pdf-design` | Step 13 (PDF QuestPDF) | Profesionalizar el layout del PDF |
| `/seo-audit` | Después de Step 19 (deploy) | Audit landing/login (cuando se agregue) |

---

## 18. Cómo ejecutar este blueprint (2 repos)

El proyecto son **2 repos separados**: `litigapp-backend` (.NET) y `litigapp-web` (Angular). Cada uno se construye en su propia sesión de Claude Code. Setup:

### Paso A — Preparar cada repo ANTES de abrir Claude Code

**En `litigapp-backend`**:
```
litigapp-backend/
├── CLAUDE.md                       ← copiar litigapp-backend-CLAUDE.md
└── docs/
    ├── blueprint.md                ← copiar ESTE blueprint completo
    └── api-rama-judicial.md         ← copiar el doc de la API Rama Judicial
```

**En `litigapp-web`**:
```
litigapp-web/
├── CLAUDE.md                       ← copiar litigapp-web-CLAUDE.md
└── docs/
    ├── blueprint.md                ← copiar ESTE blueprint completo (el mismo)
    └── mockup.tsx                   ← copiar el mockup aprobado
```

**Sí, el `blueprint.md` va completo en AMBOS repos.** Razón: el backend necesita entender qué espera el frontend (contratos de API) y el frontend necesita saber la forma exacta de la API. Es un único contrato compartido. El `CLAUDE.md` de cada repo es DISTINTO (uno backend, uno frontend) — cada uno le dice a Claude Code qué secciones del blueprint le importan.

### Paso B — Por qué CLAUDE.md hace que lea el blueprint

Claude Code lee automáticamente el `CLAUDE.md` del directorio al iniciar sesión. Ese archivo tiene al inicio la instrucción **"LO PRIMERO QUE DEBES HACER: lee docs/blueprint.md"**. Así, toda sesión arranca leyendo el blueprint sin que tengas que recordárselo. El `CLAUDE.md` NO contiene toda la info — es el índice/guía que apunta al blueprint y fija las reglas.

### Paso C — Prompt inicial para cada sesión

**Sesión backend** (abre Claude Code en `litigapp-backend/`):
```
Lee docs/blueprint.md y docs/api-rama-judicial.md completos.
Vamos a construir el backend de LitigApp siguiendo el Build Order de la
sección 11 (Steps 0 a 13). Empieza por el Step 0 (spike de la API Rama
Judicial) y para cuando lo termines para que yo lo revise antes de seguir.
```

**Sesión frontend** (abre Claude Code en `litigapp-web/`):
```
Lee docs/blueprint.md y docs/mockup.tsx completos.
Vamos a construir el frontend de LitigApp siguiendo el Build Order de la
sección 11 (Steps 14 a 18). Respeta el mockup como diseño aprobado y los
contratos de API de la sección 5. Empieza por el Step 14 (scaffolding) y
para cuando lo termines para que yo lo revise.
```

### Paso D — Orden recomendado

- **Arranca el backend primero** (al menos hasta Step 7: auth + CRUD de procesos), porque el frontend consume sus endpoints.
- Con 3 devs pueden paralelizar: el frontend puede ir avanzando con los contratos del blueprint (Step 14-15: scaffolding, layout, auth UI) mientras el backend construye los endpoints. Pero la integración real (Step 16+) necesita el backend corriendo.
- Mantén `docs/blueprint.md` sincronizado en ambos repos si haces cambios de contrato.

---

## 18.1 Contenido para `litigapp-backend/CLAUDE.md`

> Ver archivo `litigapp-backend-CLAUDE.md` generado junto a este blueprint. Cópialo tal cual al root del repo backend.

## 18.2 Contenido para `litigapp-web/CLAUDE.md`

> Ver archivo `litigapp-web-CLAUDE.md` generado junto a este blueprint. Cópialo tal cual al root del repo frontend.

## 18.3 CLAUDE.md de referencia (versión combinada — solo informativa)

> El siguiente bloque es la versión combinada original. Para los repos reales usa los dos archivos separados de arriba (18.1 y 18.2), que están adaptados a cada stack.

```markdown
# LitigApp

SaaS web + mobile para abogados litigantes en Colombia. Monitorea procesos judiciales vía API Rama Judicial y notifica al abogado por email (digest agregado por usuario). WhatsApp queda fuera del MVP y se activa en v2.

## Repos

- `litigapp-backend/` — ASP.NET 10, Clean Architecture
- `litigapp-web/` — Angular 20 + Capacitor + Tailwind

## Commands

### Backend
- `dotnet build` — build solución completa
- `dotnet run --project src/LitigApp.Api` — API en localhost:5000
- `dotnet test` — todos los tests
- `dotnet ef migrations add <Name> -p src/LitigApp.Infrastructure -s src/LitigApp.Api` — nueva migración
- `dotnet ef database update -p src/LitigApp.Infrastructure -s src/LitigApp.Api` — aplicar migraciones
- `dotnet run --project src/LitigApp.Api -- seed-catalog` — seed departamentos/municipios

### Frontend
- `pnpm start` — dev server localhost:4200
- `pnpm build` — build prod a `dist/`
- `pnpm test` — unit tests
- `pnpm e2e` — Playwright
- `pnpm lint` — ESLint con boundaries

### Mobile
- `pnpm build && npx cap sync` — sincronizar a iOS/Android
- `npx cap open ios|android` — abrir en Xcode/Android Studio

## Tech Stack

ASP.NET Core 10 (.NET 10 LTS, C# 14) + EF Core 10 + PostgreSQL 16 + Hangfire + JWT Identity + Polly v8 | Angular 20 standalone + Tailwind v4 + Capacitor 7 + lucide-angular

## Arquitectura

### Backend — Clean Architecture (5 csproj)
- `Domain` — entidades, value objects, eventos. Sin dependencias externas.
- `Application` — handlers CQRS (sin MediatR — handlers propios), validators, contratos.
- `Infrastructure` — EF Core, Identity, clientes externos (Rama Judicial, Resend, Meta), QuestPDF, ClosedXML.
- `Jobs` — Hangfire jobs: OverviewSweep (recurrente 15min), ActionsSweep (encolado), CompletePartialFetch (encolado), DispatchUserNotifications/DispatchImportComplete (triggered), NotificationFallbackSweep (recurrente cada hora), BulkImport.
- `Api` — endpoints organizados por feature (Minimal APIs con MapGroup), middleware, Program.cs.

Reglas: Domain ← nada. Application ← Domain. Infrastructure ← App+Dom. Jobs ← App+Infra+Dom. Api ← todos.

### Frontend — Angular capas con ESLint boundaries
- `core/` — singletons: auth, http, interceptors, guards.
- `shared/ui/` — componentes puros sin estado (Button, Modal, Badge, Input).
- `shared/util/` — pipes, helpers.
- `shared/domain/` — interfaces compartidas.
- `data-access/` — servicios HTTP por feature.
- `features/` — pantallas autocontenidas (auth, dashboard, process-add, etc.).

Reglas (ESLint enforza): features no se importan entre sí. shared/* no importa data-access ni features. data-access no importa features.

### Data Flow
Cliente Angular → `core/http/api-client` → API REST → Controller (Api) → Handler (Application) → Repository (Infrastructure) → EF Core → Postgres.
Job (Hangfire) → Handler (Application) → IRamaJudicialClient (Infrastructure) → API externa.

## Code Organization Rules

1. **Clean Architecture inviolable**: nunca importes Infrastructure desde Application o Domain.
2. **Un handler por carpeta**: `Commands/CreateXxx/CreateXxxCommand.cs + Handler.cs + Validator.cs`.
3. **Result<T> en todos los handlers**: nunca throw para errores esperables.
4. **Repositorios devuelven `Domain` entities**: nunca EF Core proxies fuera de Infrastructure.
5. **AsNoTracking en todas las queries de lectura**.
6. **`async` en todo I/O** con `CancellationToken` propagado.
7. **Records para DTOs** y para value objects inmutables.
8. **Migraciones EF versionadas en git**: nunca borres una migración aplicada.
9. **Tests de Domain por behavior, no por implementación**.
10. **Frontend: standalone components**. Cero NgModules.
11. **Signals para estado, Observables solo para HTTP**.
12. **Reactive Forms para todos los formularios** (no Template-driven).
13. **Tailwind utility-first**: cero CSS custom salvo el strictly necesario.
14. **lucide-angular para iconos**: no SVGs propios salvo logo.
15. **Archivos separados por componente**: `templateUrl` + `styleUrl` siempre. **Prohibido `template:`/`styles:` inline** (enforced por `@angular-eslint/component-max-inline-declarations`).
16. **Tailwind v4 CSS-first**: `@import "tailwindcss";` + `@theme` en `styles.css`. NUNCA `@tailwind base/components/utilities` (v3) ni `tailwind.config.js`.
17. **Ningún componente se da por terminado sin verificación visual**: levantar la app (`pnpm start`) y confirmar que renderiza con estilos y matchea el mockup. Plantillas sin estilos = PR incompleta.
18. **Consentimiento legal obligatorio en el registro**: no se crea cuenta sin aceptar Términos y Política (validado en backend), con versión + timestamp persistidos en `legal_acceptances`. Links a ambos documentos en footer y Settings. Ver §9.5.

## Design System

Tokens (Tailwind):
- Primary 700 `#1d4ed8` (header), Primary 600 `#2563eb` (CTA), Primary 500 `#3b82f6`.
- Background `#f8fafc` (slate-50). Surface `#ffffff`. Border `#e2e8f0` (slate-200).
- Text primary `#1e293b` (slate-800). Muted `#64748b` (slate-500).
- Warning amber-100/amber-800. Success green-50/green-700. Danger red-50/red-700. Notif red-500.

Tipografía: Inter, weights 400/500/600/700.
Radius: rounded-lg inputs/buttons, rounded-xl cards, rounded-2xl modales.
Sombras: shadow-sm cards, shadow-md header, shadow-xl modales.
Max-width: 1280px (max-w-7xl).

## Environment Variables

Ver `appsettings.json` (backend) y `environments/environment.ts` (frontend). Variables sensibles SOLO en env vars, nunca en código.

## Reglas No Negociables

1. **TypeScript strict** en frontend. **TreatWarningsAsErrors** en backend.
2. **Nunca importar Infrastructure desde Application/Domain**.
3. **Toda llamada HTTP externa va por Polly** (timeout + retry + circuit breaker).
4. **Toda mutación pasa por handlers** (no lógica en controllers).
5. **Auth obligatorio en todo endpoint excepto `/auth/*` y `/health`**.
6. **No commit de secrets**: `.env*` y `appsettings.*.json` (excepto base) en `.gitignore`.
7. **Migraciones EF nunca se editan después de aplicarse**: nueva migración encima.
8. **PDF generado con QuestPDF Community license** declarada explícitamente en Program.cs.
9. **WAF awareness**: 403 → cooldown → resume. Trickle + jitter + UA rotation siempre.
10. **Solo 2 endpoints diarios**: overview + actions. detail/subjects solo en creación inicial.
11. **Notificaciones agregadas por usuario, NUNCA por proceso**.
12. **Despacho de notificaciones es triggered**, no polling. Solo fallback hourly.
13. **WhatsApp FUERA del MVP** — stub `NoOpWhatsAppSender` registrado.
14. **BrightData NO activo en MVP** — `Proxy.Enabled=false`.
15. **Excel imports**: 2MB / 5000 filas máximo enforced en controller.
16. **Hangfire retention 24h** explícito.
17. **Creación individual SÍNCRONA**: no Hangfire para 1 proceso.
18. **Bloqueo mutuo import ↔ creación individual**: 409 desde backend.
19. **Partial sync UI guard**: si `syncStatus != 'ok'`, PDF deshabilitado + banner.
20. **Tests de integración con Testcontainers Postgres real**, no SQLite.
21. **Todos los parámetros WAF/sync son configurables** por env vars.
```

---

## 19. Reglas No Negociables

1. **Clean Architecture inviolable en backend**. Las referencias entre csproj NUNCA se invierten. Si hay duda, detener y reconsultar.
2. **Frontend con ESLint boundaries activas desde el primer commit**. Si un boundary falla en CI, no se mergea.
3. **CQRS sin MediatR**: handlers propios, sin dependencias con licencia comercial.
4. **Toda llamada a API externa pasa por Polly**: timeout + retry + circuit breaker, no negociable.
5. **WAF awareness en todas las llamadas a Rama Judicial**: 403 → `FailureKind.WafBlocked` → set `waf_blocked_until` → abort sweep. Jamás ignorar o reintentar un 403 inmediatamente.
6. **Trickle + jitter + User-Agent rotation** en TODAS las llamadas a Rama Judicial. NUNCA ráfagas, NUNCA mismo User-Agent fijo.
7. **Solo 2 endpoints en el ciclo diario**: overview + actions. detail/subjects SOLO en creación inicial. Confirmado con negocio que no cambian.
8. **Notificaciones SIEMPRE agregadas por usuario, NUNCA por proceso**. Si 10 procesos del usuario X cambian, se envía UN solo email con los 10.
9. **Despacho de notificaciones es TRIGGERED, no polling cada 5 min**. Solo `NotificationFallbackSweepJob` corre hourly y solo para huérfanos.
10. **Creación individual de proceso = SÍNCRONA**: NO usar Hangfire para 1 solo proceso. Hangfire solo para fallback parcial via `CompletePartialFetchJob`.
11. **Bloqueo mutuo importación ↔ creación individual**: mientras hay `import_jobs` activo del usuario, `POST /processes/*` devuelve 409. El frontend deshabilita el botón.
12. **Bulk import respeta el WAF**: si recibe 403, pausa el job (status='paused'), re-encolar tras cooldown. El frontend lo refleja.
13. **WhatsApp FUERA del MVP**. Solo interfaz `IWhatsAppSender` + stub `NoOpWhatsAppSender`. La columna `whatsapp_enabled` default FALSE.
14. **BrightData / proxy NO se activa en MVP**. `Proxy.Enabled=false`. La infraestructura `FailoverRamaJudicialClient` queda lista para activarse vía config en Tier 2.
15. **PDF guard**: si `sync_status != 'ok'` el endpoint PDF devuelve 409 y el frontend deshabilita el botón.
16. **Excel imports tienen límites duros**: 2MB / 5000 filas. Validación ANTES de ClosedXML para evitar OOM.
17. **Hangfire retention 24h** explícito en config. Cleanup jobs semanales para outbox y import_jobs.
18. **Idempotencia en jobs**: UNIQUE en `external_action_id` para actuaciones. Cada job nuevo debe diseñarse idempotente.
19. **Soft delete, no hard delete** en `processes`. Solo Admin puede hard delete.
20. **JWT secret mínimo 32 chars**. Rotación obligatoria si se filtra.
21. **No commit de secrets**. `.env`, `appsettings.Development.json`, `appsettings.Production.json` en `.gitignore`.
22. **Migraciones EF NUNCA se editan después de aplicarse**. Nueva migración encima.
23. **TypeScript strict** + **TreatWarningsAsErrors** en C#. Siempre.
24. **Tests de integración con Testcontainers Postgres real**, no SQLite.
25. **El mockup `litigapp_mockup.tsx` es la fuente de verdad visual**.
26. **Mobile = Angular + Capacitor, NO Ionic**.
27. **No broker real (RabbitMQ/Kafka) hasta Tier 2**. `IMessagePublisher` ya lo permite.
28. **No Stripe / pagos en MVP**. Cobro manual.
29. **No push notifications nativas en MVP**. Email cubre.
30. **PDF construido desde data en BD** (endpoint Documentos de Rama Judicial no funciona).
31. **Sync de actuaciones SOLO página 1**. Sin safety net de paginación adicional.
32. **`User.Id` en backend es text (Identity convention)**, NO uuid.
33. **Versiones LTS más recientes**: .NET 10 + Angular 20.
34. **Docker SOLO obligatorio para producción**. En dev es opcional via docker-compose.
35. **Todos los parámetros del WAF strategy son configurables** vía env vars: throttle, cooldown, sweep interval, batch size. NO hardcodear.
