# LitigApp — Plan de Ejecución por Verticales (3 devs full-stack)

> Cómo nos repartimos el trabajo para construir el MVP. Cada persona es **owner de UNA vertical de producto end-to-end** (backend + frontend). Todos tocamos los dos repos.

---

## Cómo trabajamos

- **Stack**: backend .NET 10 (`litigapp-backend`) + frontend Angular 20 (`litigapp-web`). 2 repos separados.
- **Fuente de verdad**: `docs/blueprint.md` en cada repo. Todo lo que construyamos sigue ese build order (sección 11 del blueprint, 20 steps numerados).
- **Branching**: feature branches desde `main`. Nombre `feature/<vertical>-<descripcion-corta>` (ej: `feature/account-jwt-auth-backend`, `feature/portfolio-dashboard-tabs`).
- **Merge**: solo **Squash and merge**. Títulos Conventional Commits (`feat(api): ...`, `feat(web): ...`, `fix:`, `chore:`, etc.). El scope `api` o `web` ayuda a leer el log.
- **Review**: cada PR requiere mínimo 1 approval de otro dev. `main` protegida en ambos repos.
- **CI**: cada PR corre `dotnet build + test` (backend) o `pnpm lint + build + test` (frontend) automático.
- **Daily**: 15 min, todos los días a la misma hora. Cada uno: qué cerré, qué abro hoy, qué me bloquea.
- **Comms async**: Slack/Discord. Si vas a tocar un archivo grande compartido (`Program.cs`, `appsettings.json`, `app.config.ts`), avisa antes.

## Las 3 verticales

| Vertical | Owner | Foco |
|---|---|---|
| **Cuenta + Infra** | Cristian Montes | Auth, Settings, WhatsApp stub, Capacitor mobile, CI/CD, deploy, observabilidad |
| **Portafolio de Procesos** | Santiago Guitierrez | Catalog, Process CRUD, Dashboard (tabs Novedades/Procesos), Detail dialog, Wizard de creación, PDF |
| **Engine de Monitoreo + Importación** | Sergio Molina | API Rama Judicial, Sync engine, Email digest, Excel imports (back + UI), partial sync handling |

> Ajusta los nombres A, B, C según el equipo real. La idea: cada persona OWNED su vertical end-to-end, no se divide por capas.
  ** A : Cristian Montes
  ** B : Santiago Guitierrez
  ** C : Sergio Molina

### Qué significa "owner end-to-end"

Si eres owner de la vertical:
- Tú escribes el backend (entidades, handlers, endpoints).
- Tú escribes el frontend (componentes, servicios, integración).
- Tú escribes los tests.
- Tú abres los PRs (un PR puede ser backend-only, frontend-only o mixto según convenga).
- Cuando alguien necesita algo de tu vertical, eres el contacto.

---

## Timeline general

| Sprint | Duración | Foco principal |
|---|---|---|
| 0 | 2-3 días | Setup compartido (scaffolding + CI + BD) |
| 1 | 1 semana | Cimientos de cada vertical en paralelo |
| 2 | 1.5 semanas | Conexiones cruzadas + features visibles |
| 3 | 2 semanas | Sync engine + dashboard completo |
| 4 | 1.5 semanas | Imports + mobile + email digest |
| 5 | 1 semana | Deploy + beta con abogados reales |

**Total estimado: ~7 semanas al MVP funcional en producción.**

---

## Sprint 0 — Setup compartido (2-3 días)

> **Lo hace A solo.** Los demás clonan, configuran su entorno y se leen el blueprint.

| # | Tarea | Owner | Repo |
|---|---|---|---|
| 0.1 | Backend scaffolding (PR #1) — solution + 5 csproj + Directory.Build.props + docker-compose | A | backend |
| 0.2 | Frontend scaffolding (PR #1) — Angular 20 + Tailwind v4 + tokens del design system + Capacitor + PWA + estructura de carpetas | A | frontend |
| 0.3 | Branch protection en `main` (ambos repos) + squash-only + 1 approval mínimo | A | both |
| 0.4 | GitHub Actions CI en ambos repos (build + test + lint) | A | both |
| 0.5 | Postgres + EF Core + migración inicial vacía (todas las tablas del esquema sección 4 del blueprint) | A | backend |
| 0.6 | Setup Slack/Discord + GitHub Project board con los 20 steps + esta lista | A | infra |
| 0.7 | Cada uno: clonar repos, instalar Node 24 + pnpm + .NET 10 SDK + Docker, verificar `dotnet build` y `pnpm build` | A, B, C | local |
| 0.8 | Cada uno: leer `docs/blueprint.md` completo + sección de su vertical en detalle | A, B, C | lectura |

**Salida**: ambos repos clonables, buildables, con CI, branch protection activa, schema inicial de BD aplicado. Todos saben qué vertical les toca y pueden arrancar.

**Importante**: la migración inicial (0.5) debe incluir TODAS las tablas que cada vertical va a usar — `users`, `processes`, `process_actions`, `process_subjects`, `courts`, `departments`, `cities`, `entities`, `specialties`, `notifications_outbox`, `notification_logs`, `import_jobs`, `user_notification_preferences`, `sync_state`. Así nadie se pisa con migraciones después. Cada vertical mete su lógica sobre tablas ya creadas; si necesitan ALTER, son migraciones puntuales coordinadas.

---

## Sprint 1 — Cimientos por vertical (1 semana, paralelo total)

Cada uno arranca su vertical con la pieza que **NO depende de los demás**:

| # | Tarea | Owner | Repo | Step blueprint | Notas |
|---|---|---|---|---|---|
| 1.A | Auth backend completo: `ApplicationUser`, Identity setup, JWT service, endpoints `register/login/refresh/password-reset` | A | backend | 4 | base para que B y C puedan poner `[Authorize]` después |
| 1.B | Catalog backend completo: seeder de departamentos + municipios DANE + entidades + especialidades + endpoints REST `/catalog/*` | B | backend | 3 + catalog endpoints | desbloquea el wizard de creación |
| 1.C | API Spike (1-2 días) + `IRamaJudicialClient` con DTOs + Polly + WAF detection + User-Agent rotation | C | backend | 0 + 6 | desbloquea Process CRUD de B y todo el sync engine |

**Checkpoint final de sprint**: las 3 piezas mergeadas en main. Cero conflictos porque cada uno trabaja en sus propios archivos.

**Zonas compartidas a coordinar**:
- `Program.cs`: A registra Identity/JWT, B registra Catalog services, C registra HttpClient + Polly. Cada uno mete su `services.AddXxx()` en su PR. Si conflicto, el segundo en mergear rebasea.
- `appsettings.json`: A agrega `Jwt`, B nada, C agrega `RamaJudicial`. Mismo patrón.

---

## Sprint 2 — Conexiones cruzadas + features visibles (1.5 semanas)

Ahora se mezclan. Cada uno consume lo de los otros.

| # | Tarea | Owner | Repo | Step | Bloqueado por |
|---|---|---|---|---|---|
| 2.A | Auth UI frontend: login + register + forgot-password + reset-password screens + `AuthService` con signals + `JwtInterceptor` + `authGuard`/`guestGuard` | A | frontend | 15 | 1.A merged |
| 2.B | Dashboard shell + Process CRUD backend (`POST /processes/full-number`, `POST /processes/wizard`, `GET /processes/novelties`, `GET /processes`, `GET /processes/{id}`, `POST /processes/{id}/mark-attended`) SÍNCRONO con fallback partial | B | both | 7 + 8 + shell de 16 | 1.A (auth para `[Authorize]`) + 1.B (catalog) + 1.C (Rama Judicial client) |
| 2.C | Hangfire setup + dashboard `/hangfire` con auth Admin + arranca `OverviewSweepJob` (versión inicial sin adaptive throttling todavía) | C | backend | 5 + parte de 10 | 1.C (cliente Rama Judicial) |

**Checkpoint final de sprint**:
- Un abogado puede registrarse, hacer login y ver el shell del dashboard vacío con tabs.
- Un abogado puede crear un proceso manualmente (full-number o wizard) y persistirlo con info real de la API Rama Judicial.
- Hangfire dashboard accesible, sync engine corriendo en seco (sin acciones todavía).

**B tiene la carga más alta este sprint** (Process CRUD es grande y depende de A y C). Por eso 1.A y 1.C deben mergearse pronto en Sprint 1, ojalá en los primeros 3 días, para que B tenga tiempo de codear con todo desbloqueado.

**Zonas compartidas**:
- `dashboard.component.html`: B lo crea. C y A no lo tocan en este sprint.
- `app.routes.ts`: A agrega rutas auth, B agrega rutas dashboard. Coordinar en Slack si chocan.

---

## Sprint 3 — Sync engine + dashboard completo (2 semanas)

| # | Tarea | Owner | Repo | Step | Bloqueado por |
|---|---|---|---|---|---|
| 3.A | Settings UI: profile form + notification preferences (toggle email; WhatsApp deshabilitado con "Próximamente") + cambio de contraseña + WhatsApp stub backend (`IWhatsAppSender` + `NoOpWhatsAppSender`) + `docs/v2-whatsapp-integration.md` | A | both | 17 settings + 12 | 1.A + 2.A |
| 3.B | Tab Novedades + Tab Procesos (filtros: juzgado / radicado / sujetos) + paginación + integración real con backend | B | frontend | 16a + 16b | 2.B |
| 3.B' | Detail dialog: vista completa + actuaciones con grouping Auto+Fijación + "Marcar atendido" con mutación optimista + partial sync UI guard + polling cada 10s si `syncStatus='partial'` | B | frontend | 16c | 2.B |
| 3.B'' | PDF backend: QuestPDF document + endpoint `GET /processes/{id}/pdf` con guard `409` si `sync_status != 'ok'` | B | backend | 13 | 2.B |
| 3.B''' | Wizard UI de creación: stepper depto → ciudad → despacho → consecutivo + integración con catálogo backend | B | frontend | 17 wizard | 1.B + 2.B |
| 3.C | Sync engine completo: `sync_state` table + `OverviewSweepJob` con WAF cooldown + `ActionsSweepJob` triggered + `CompletePartialFetchJob` + adaptive throttling + idempotencia | C | backend | 10 | 2.B (Process entity completa) + 2.C |

**Checkpoint final de sprint**:
- Dashboard 100% funcional visualmente: ambas tabs operativas, detail dialog completo, wizard funciona, PDF se descarga.
- Sync engine corriendo cada 15 min, respeta WAF, marca procesos como `attended=false` cuando detecta novedades. **AÚN SIN ENVIAR EMAIL** (eso es Sprint 4).
- Settings funcional.

**3.C es la tarea más compleja del proyecto.** Recomendación a C:
- Usar **Opus 4.7** para esa sesión (no Sonnet 4.6).
- Arrancar con `/autoplan` + `/plan-eng-review` antes de codear.
- PR puede salir dividido si crece mucho: `sync_state + columnas + state machine` → `OverviewSweepJob` → `ActionsSweepJob + adaptive throttle` → `CompletePartialFetchJob`.

**B tiene 4 sub-tareas (3.B, 3.B', 3.B'', 3.B''')** porque le toca todo el dashboard. Lo aprovecha para producir 4 PRs pequeños y reviewables en lugar de uno gigante.

---

## Sprint 4 — Imports + mobile + email digest (1.5 semanas)

| # | Tarea | Owner | Repo | Step | Bloqueado por |
|---|---|---|---|---|---|
| 4.A | Capacitor mobile: build iOS + Android + íconos + splash + permisos + verificación de que el flow login → dashboard → ver proceso funciona en device/emulador | A | frontend | 18 | 2.A + 3.B (UI completa) |
| 4.A' | Setup Supabase prod (proyecto + extensiones + connection string + backups) + Railway services (`litigapp-api` con Dockerfile.api + `litigapp-worker` con Dockerfile.worker) + Vercel deploy frontend + DNS preliminar | A | infra | 19 parcial | nada — puede arrancar el sprint con esto |
| 4.B | Tests E2E con Playwright para flujos críticos (registro → login → crear proceso → marcar atendido → descargar PDF) + verificación de bloqueo `import_jobs` activo en endpoints `POST /processes/*` | B | frontend | 15 verificación | 3.B + 4.C (parcial, para test del import) |
| 4.C | Imports backend completo: endpoint preview + execute + `BulkImportJob` + límites (2MB / 5000 filas) + `GET /imports/active` + bloqueo mutuo con creación individual + integración con sync engine (procesos importados quedan `attended=true` y sync engine los procesa después) | C | backend | 9 | 3.C |
| 4.C' | Import UI frontend: upload + mapping de columnas + progress banner global + polling cada 3s + popup de finalización + redirect a `/processes` con reload | C | frontend | 17 import | 4.C |
| 4.C'' | Email digest: `ResendEmailSender` + templates HTML (UserDigest + ImportComplete) + `DispatchUserNotificationsJob` triggered desde sync + `DispatchImportCompleteJob` triggered desde import + `NotificationFallbackSweepJob` hourly + Hangfire retention 24h | C | backend | 11 | 3.C + 4.C |

**Checkpoint final de sprint**:
- App funcionalmente completa: importación masiva end-to-end, emails llegan en formato digest tras sync diario, apps móviles compilan localmente.
- Tests E2E verdes.
- Infraestructura prod montada pero sin tráfico real todavía.

---

## Sprint 5 — Deploy producción + beta (1 semana)

| # | Tarea | Owner | Repo | Step | Notas |
|---|---|---|---|---|---|
| 5.1 | Deploy oficial a Vercel + Railway + Supabase con dominios `app.litigapp.co` + `api.litigapp.co` + variables de entorno + verificación de health checks | A | infra | 19 | |
| 5.2 | Rate limiting (`AspNetCoreRateLimit`) + Serilog → Logtail/Better Stack + Sentry en backend y frontend + alertas críticas configuradas | A | both | 20 | |
| 5.3 | Smoke tests en producción: registro real, login, crear proceso, esperar al sync diario, verificar email llega | A, B, C | producción | — | |
| 5.4 | Onboarding de 2-3 abogados beta: crear sus cuentas, ayudarles a importar su Excel, dejar la app corriendo 1 semana | A, B, C | beta | — | |
| 5.5 | Monitoreo activo: WAF cooldowns, emails entregados, errores en Sentry, latencia API. Hotfixes prioritarios | A, B, C | producción | — | |

**Salida**: MVP en producción con primeros usuarios reales. Métricas para tomar decisiones de Tier 1 (cuándo subir Supabase Pro, activar más workers, etc.).

---

## Mapa visual de dependencias

```
Sprint 0 (A solo)
  Scaffolding + DB inicial
        ↓
Sprint 1 (paralelo total)
  ┌─────────────┬─────────────┬──────────────┐
  ↓             ↓             ↓
[1.A] Auth   [1.B] Catalog  [1.C] Rama Judicial
backend      backend        Client + Spike
  │             │             │
  ↓             ↓             ↓
Sprint 2
  ┌─────────────┬──────────────────────────┐
  ↓             ↓                          ↓
[2.A] Auth   [2.B] Process CRUD       [2.C] Hangfire
UI           + Dashboard shell        + sync inicial
                  ↑ ↑
              depende de 1.A + 1.B + 1.C
  │             │                          │
  ↓             ↓                          ↓
Sprint 3
  ┌─────────────┬──────────────────────────┐
  ↓             ↓                          ↓
[3.A] Settings [3.B*] Dashboard         [3.C] Sync engine
+ WhatsApp     completo (tabs +         completo
stub           detail + wizard + PDF)
                                            ↓
                                       (depende de 2.B
                                        para Process entity)
  │             │                          │
  ↓             ↓                          ↓
Sprint 4
  ┌─────────────┬──────────────────────────┐
  ↓             ↓                          ↓
[4.A] Mobile  [4.B] E2E tests           [4.C] Imports +
+ Infra prod                              Import UI +
                                          Email digest
                                          ↑
                                     depende de 3.C
  │             │                          │
  └─────────────┴─────────┬────────────────┘
                          ↓
Sprint 5
  Todos: Deploy + Beta + Monitoreo
```

---

## Reglas de oro de las dependencias

1. **Sprint 1 termina cuando las 3 fundaciones (1.A, 1.B, 1.C) están mergeadas.** Hasta entonces, nadie arranca Sprint 2.
2. **2.B (Process CRUD) es el cuello de botella del Sprint 2** — bloquea casi todo lo de Sprint 3. Que B se enfoque ahí sin distracciones.
3. **3.C (Sync engine) bloquea todo Sprint 4 de C** — si C se atrasa, C no llega a imports + email. En ese caso, **B puede tomar imports** o **A puede tomar email** como rescate. Decidir en daily.
4. **Si te bloquea otra vertical**: en lugar de esperar pasivo, agarra una tarea de tu siguiente sprint que NO esté bloqueada, o ayuda con un PR en review.

---

## Tareas asignadas — vista por persona

### Persona A — Cuenta + Infra
**Sprint 0**: 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8
**Sprint 1**: 1.A (Auth backend)
**Sprint 2**: 2.A (Auth UI frontend)
**Sprint 3**: 3.A (Settings UI + WhatsApp stub backend)
**Sprint 4**: 4.A (Capacitor mobile) + 4.A' (Infra prod prep)
**Sprint 5**: 5.1, 5.2, 5.3, 5.4, 5.5

**Total**: ~12 tareas mayores. Carga media-alta porque también es la lead técnica e infra owner.

### Persona B — Portafolio de Procesos
**Sprint 0**: 0.7, 0.8
**Sprint 1**: 1.B (Catalog backend)
**Sprint 2**: 2.B (Dashboard shell + Process CRUD backend) — **el sprint más pesado para B**
**Sprint 3**: 3.B + 3.B' + 3.B'' + 3.B''' (Dashboard completo: tabs + detail + PDF + Wizard) — **el otro sprint pesado**
**Sprint 4**: 4.B (E2E tests + verificaciones cross-vertical)
**Sprint 5**: 5.3, 5.4, 5.5

**Total**: ~9 tareas mayores. Sprints 2 y 3 son intensos en UI work.

### Persona C — Engine de Monitoreo + Importación
**Sprint 0**: 0.7, 0.8
**Sprint 1**: 1.C (API Spike + Rama Judicial Client)
**Sprint 2**: 2.C (Hangfire setup + sync inicial)
**Sprint 3**: 3.C (Sync engine completo) — **2 semanas casi exclusivas**
**Sprint 4**: 4.C + 4.C' + 4.C'' (Imports backend + Import UI + Email digest)
**Sprint 5**: 5.3, 5.4, 5.5

**Total**: ~8 tareas mayores pero las más complejas técnicamente. Sprint 3 es muy denso (sync engine) — considera Opus 4.7 + sub-PRs.

---

## Reglas de proceso

1. `main` siempre buildea y tests pasan. Si CI rojo en main, prioridad máxima del último merger.
2. **PRs pequeños**: < 800 LOC ideal. Si crece, divide en sub-PRs.
3. Antes de codear una tarea del blueprint, **léete la sección correspondiente completa**. No improvises.
4. Si detectas que el blueprint está mal o incompleto, **detén el work**, escribe en Slack, actualizamos blueprint en ambos repos, y luego sigues.
5. **Tests obligatorios** para handlers backend (unit) y endpoints críticos (integración con Testcontainers).
6. **Tests E2E solo para flujos críticos**, no para todo.
7. **Code review**: mínimo 1 approval. Comentarios resueltos antes de merge.
8. **`/review` con gstack antes de pedir review humana** — atrapa errores baratos.
9. **`/ship` para commit + push + PR**.
10. **Modelo Claude por defecto Sonnet 4.6**. Subir a Opus 4.7 para: sync engine (3.C), debugging difícil, decisiones fuera del blueprint, review crítico.

---

## Comunicación cuando hay bloqueos

Si te bloquea otro vertical:
```
🚫 BLOQUEADO en [tarea] porque necesito [pieza] de la vertical de @persona.
ETA?
```

Si te bloquea infra externa (Rama Judicial, Supabase, etc):
```
⚠️ BLOQUEO EXTERNO en [tarea]: [qué pasó].
Hipótesis / próximos pasos: [...]
```

Si te bloquea el blueprint:
```
❓ DUDA BLUEPRINT sección [N]: [qué no entiendes / qué parece mal].
Propuesta: [...]
```

Las dudas del blueprint las resolvemos en el daily o en el canal según urgencia.
