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

La API queda disponible en `http://localhost:5000`.

## Comandos útiles

| Comando | Descripción |
|---------|-------------|
| `dotnet build` | Compila la solución completa |
| `dotnet test` | Corre todos los tests |
| `dotnet run --project src/LitigApp.Api` | Inicia la API en localhost:5000 |
| `docker compose up -d` | Levanta Postgres 16 en el puerto 5432 |
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

## Variables de entorno

Copia `appsettings.Development.json.example` (cuando exista) a `appsettings.Development.json` y rellena los valores. Los secretos nunca van en código ni en git.

```
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=litigapp;Username=litigapp;Password=litigapp_dev
```

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
