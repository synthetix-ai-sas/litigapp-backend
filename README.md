# LitigApp — Backend

API REST en .NET 10 para monitoreo automático de procesos judiciales en Colombia vía la API de la Rama Judicial.

## Requisitos previos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (para Postgres local)

## Setup rápido

```bash
# 1. Clona el repo
git clone <url>
cd litigapp-backend

# 2. Levanta Postgres local
docker compose up -d

# 3. Restaura dependencias y compila
dotnet build

# 4. Corre la API
dotnet run --project src/LitigApp.Api
```

La API queda disponible en `http://localhost:5119`.

## Comandos útiles

| Comando | Descripción |
|---------|-------------|
| `dotnet build` | Compila la solución completa |
| `dotnet test` | Corre todos los tests |
| `dotnet run --project src/LitigApp.Api` | Inicia la API en localhost:5119 |
| `docker compose up -d` | Levanta Postgres 16 en el puerto 5433 |
| `docker compose down` | Detiene los contenedores |
| `dotnet ef migrations add <Nombre> -p src/LitigApp.Infrastructure -s src/LitigApp.Api` | Crea una migración |
| `dotnet ef database update -p src/LitigApp.Infrastructure -s src/LitigApp.Api` | Aplica migraciones |

## Estructura del proyecto

```
litigapp-backend/
├── src/
│   ├── LitigApp.Domain/         # Entidades, value objects, eventos — sin dependencias externas
│   ├── LitigApp.Application/    # Handlers CQRS, interfaces, validators
│   ├── LitigApp.Infrastructure/ # EF Core, Identity, clientes externos, Resend, QuestPDF
│   ├── LitigApp.Jobs/           # Hangfire jobs (sync, notificaciones, mantenimiento)
│   └── LitigApp.Api/            # Controllers, middleware, Program.cs
└── tests/
    ├── LitigApp.Domain.UnitTests/
    ├── LitigApp.Application.UnitTests/
    └── LitigApp.Api.IntegrationTests/
```

**Regla de dependencias (Clean Architecture — inviolable):**
```
Domain ← Application ← Infrastructure ← Jobs ← Api
```

## Variables de entorno y config local

`appsettings.json` (versionado) tiene los valores no-secretos de **producción**; Railway solo inyecta secretos por env vars. Cada dev mantiene su propio `src/LitigApp.Api/appsettings.Development.json` (no versionado) con sus overrides locales. Mínimo para correr en local:

```jsonc
{
  "ConnectionStrings": {
    "Postgres": "Host=localhost;Port=5433;Database=litigapp;Username=litigapp;Password=litigapp_dev"
  },
  "Jwt": { "Secret": "<min-32-chars>" },
  "Cors": { "AllowedOrigins": [ "http://localhost:4200", "https://localhost:4200", "capacitor://localhost" ] }
}
```

Los secretos nunca van en código ni en git.

`launchSettings.json` sí está versionado, con `RESEND_APITOKEN` como placeholder. Tras reemplazarlo por tu token real, corre `git update-index --skip-worktree src/LitigApp.Api/Properties/launchSettings.json` para que git no marque el cambio.

## Tech stack

- **Runtime:** .NET 10 / C# 14
- **ORM:** Entity Framework Core 10 + PostgreSQL 16
- **Auth:** ASP.NET Core Identity + JWT Bearer
- **Jobs:** Hangfire
- **Resiliencia:** Polly v8
- **Email:** Resend
- **PDF:** QuestPDF
- **Excel:** ClosedXML
- **Tests:** xUnit + FluentAssertions + Testcontainers
