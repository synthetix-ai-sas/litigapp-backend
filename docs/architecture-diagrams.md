# LitigApp — Diagramas de Arquitectura

> Formato Mermaid. Cómo usarlos:
> - **mermaid.live**: pega el bloque (sin los ```) en https://mermaid.live → exporta PNG/SVG.
> - **draw.io**: Arrange → Insert → Advanced → Mermaid → pega el código.
> - **Notion / GitHub / Obsidian**: renderizan Mermaid nativo en bloques ```mermaid.

---

## 1. Arquitectura de Sistema (vista de despliegue)

```mermaid
flowchart TB
    subgraph Client["Cliente"]
        Web["Angular 20 SPA<br/>(navegador)"]
        PWA["PWA instalable"]
        Mobile["Apps iOS / Android<br/>(Capacitor 7)"]
    end

    subgraph VercelHost["Vercel (Free)"]
        NG["Angular build estático<br/>+ CDN global"]
    end

    subgraph RailwayHost["Railway"]
        API["LitigApp.Api<br/>REST + Auth JWT<br/>(Dockerfile.api)"]
        Worker["LitigApp Worker<br/>Hangfire jobs<br/>(Dockerfile.worker)"]
    end

    subgraph SupabaseHost["Supabase"]
        PG[("PostgreSQL 16<br/>app · hangfire · outbox · sync_state")]
    end

    subgraph ExternalSvc["Servicios Externos"]
        Rama["API Rama Judicial<br/>(WAF / Cloudflare)"]
        Resend["Resend<br/>(email digest)"]
        BrightData["BrightData proxy<br/>(Tier 2+, inactivo en MVP)"]
    end

    Web --> NG
    PWA --> NG
    Mobile --> NG
    NG -->|HTTPS + JWT Bearer| API
    API <-->|EF Core| PG
    Worker <-->|EF Core + Hangfire storage| PG
    API -->|creación individual SÍNCRONA<br/>trickle + UA rotation| Rama
    Worker -->|sync diario<br/>trickle + 403 cooldown| Rama
    Worker -.->|failover Tier 2| BrightData
    BrightData -.-> Rama
    Worker -->|1 email digest por usuario| Resend
    Resend -->|notificación| Client
```

---

## 2. Sync Engine WAF-resilient (flujo del job diario)

```mermaid
flowchart TD
    Start([Cron cada 15 min · configurable]) --> Check{WAF cooldown<br/>activo?}
    Check -->|Sí| Skip[Skip corrida · log]
    Check -->|No| Load["Cargar batch 500 procesos<br/>WHERE sync_phase IN pending_overview, idle<br/>ORDER BY last_sync_attempt_at ASC"]
    Load --> Loop["Para cada proceso:<br/>sleep 2-3s + jitter<br/>rotar User-Agent"]
    Loop --> Overview[GET overview · NumeroRadicacion]
    Overview --> Result{Resultado}
    Result -->|403 WAF| Block["Set waf_blocked_until = now+20min<br/>ABORT corrida<br/>restantes quedan pending_overview"]
    Result -->|fechaUltimaActuacion cambió| MarkActions[sync_phase = pending_actions]
    Result -->|sin cambios| Idle[sync_phase = idle<br/>last_synced_at = now]
    Result -->|no existe| NotFound[sync_status = not_found]
    Result -->|error 5xx tras Polly| Err[sync_status = error<br/>sync_attempts++]
    MarkActions --> Loop
    Idle --> Loop
    NotFound --> Loop
    Err --> Loop
    Loop -->|fin del batch| HasActions{Hay procesos<br/>pending_actions?}
    HasActions -->|No| EndA([Fin])
    HasActions -->|Sí| Enqueue[Enqueue ActionsSweepJob]
    Enqueue --> ActionsJob["ActionsSweepJob<br/>GET actuaciones pág.1 de cada cambiado<br/>(mismo trickle + 403 handling)"]
    ActionsJob --> Diff["Diff por consecutive_number<br/>+ grouping Auto+Fijación<br/>attended = false"]
    Diff --> Collect[Acumular changedUsers]
    Collect --> Dispatch["Por cada usuario con cambios:<br/>Enqueue DispatchUserNotificationsJob"]
    Dispatch --> Email["1 email digest por usuario<br/>(tabla con N procesos)<br/>vía Resend"]
    Email --> EndB([Fin])
```

---

## 3. Flujo de creación individual de proceso (SÍNCRONO)

```mermaid
flowchart TD
    Start([POST /processes/full-number]) --> ValImport{Hay import<br/>activo?}
    ValImport -->|Sí| C409[409 IMPORT_IN_PROGRESS]
    ValImport -->|No| ValDup{Duplicado<br/>user+radicado?}
    ValDup -->|Sí| C409b[409 DUPLICATE_PROCESS]
    ValDup -->|No| Overview[GET overview]
    Overview --> OvRes{Existe?}
    OvRes -->|No / 200 vacío| C422[422 PROCESS_NOT_FOUND]
    OvRes -->|403 WAF| Partial1["Guardar mínimo<br/>sync_phase=pending_partial_completion<br/>encolar CompletePartialFetchJob"]
    OvRes -->|Sí| Detail[GET detalle]
    Detail --> Subjects[GET sujetos]
    Subjects --> Actions[GET actuaciones pág.1]
    Actions --> AnyFail{Algún call<br/>falló?}
    AnyFail -->|Sí| Partial2["Persistir lo obtenido<br/>sync_status=partial<br/>encolar CompletePartialFetchJob"]
    AnyFail -->|No| Full["Persistir todo en 1 transacción<br/>sync_status=ok · attended=true"]
    Partial1 --> Resp[201 Created]
    Partial2 --> Resp
    Full --> Resp
    Resp --> Modal["Frontend muestra modal<br/>con info del proceso"]
```

---

## 4. Modelo de datos (entidades principales)

```mermaid
erDiagram
    AspNetUsers ||--o{ processes : "owns"
    AspNetUsers ||--|| user_notification_preferences : "has"
    processes ||--o{ process_actions : "has"
    processes ||--o{ process_subjects : "has"
    processes }o--|| courts : "belongs to"
    courts }o--|| cities : "in"
    cities }o--|| departments : "in"
    courts }o--o| entities : "entity"
    courts }o--o| specialties : "specialty"
    AspNetUsers ||--o{ notifications_outbox : "queued for"
    AspNetUsers ||--o{ import_jobs : "runs"
    notifications_outbox ||--o{ notification_logs : "logged"

    processes {
        uuid id PK
        text user_id FK
        char file_number "23 dígitos"
        bigint external_process_id
        text current_status
        timestamptz last_court_action_at
        timestamptz last_synced_at
        timestamptz last_sync_attempt_at
        int last_external_consecutive
        text sync_status
        text sync_phase
        bool attended
        bool is_active
    }
    process_actions {
        uuid id PK
        uuid process_id FK
        bigint external_action_id "UNIQUE"
        int consecutive_number
        date action_date
        text action
        date recorded_at
        uuid is_grouped_with
    }
    process_subjects {
        uuid id PK
        uuid process_id FK
        text subject_type
        text name
        text source "api|manual"
    }
    courts {
        uuid id PK
        char official_code "12 dígitos"
        int city_id FK
        text name
    }
    notifications_outbox {
        uuid id PK
        text user_id FK
        text event_type
        text channel "email"
        jsonb payload
        text status
    }
    sync_state {
        text key PK
        timestamptz value_timestamp
        text reason
    }
```

---

## 5. Topología de repos y despliegue

```mermaid
flowchart LR
    subgraph Repos["GitHub (2 repos)"]
        BE["litigapp-backend<br/>.NET solution<br/>docs/blueprint.md<br/>docs/api-rama-judicial.md"]
        FE["litigapp-web<br/>Angular workspace<br/>docs/blueprint.md<br/>docs/mockup.tsx"]
    end

    BE -->|push main| RailwayCI["Railway<br/>build Dockerfile.api + .worker"]
    FE -->|push main| VercelCI["Vercel<br/>pnpm build"]

    RailwayCI --> APIsvc["Servicio: litigapp-api"]
    RailwayCI --> WORKERsvc["Servicio: litigapp-worker"]
    VercelCI --> CDNsvc["app.litigapp.co"]

    APIsvc --> SupaCI[("Supabase Postgres")]
    WORKERsvc --> SupaCI
    APIsvc -->|"api.litigapp.co"| DNS["DNS / Dominio"]
    CDNsvc --> DNS
```
