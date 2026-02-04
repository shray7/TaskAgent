# Evaluation Guide

This document addresses what we're looking for: **clean structure**, **architectural decisions**, **trade-offs**, **frontend–backend communication**, **production readiness**, and **documentation/setup**.

---

## 1. Clean, Well-Structured Code

### Backend (.NET)

- **Layered structure:** API → Contracts (DTOs, requests) → DataAccess (EF Core, DbContext). Controllers stay thin; business logic lives in services (`IProjectsService`, `ITasksService`, etc.).
- **Shared contracts:** `TaskAgent.Contracts` holds DTOs and request models so API and clients agree on shapes. Validation lives in dedicated FluentValidation validators; a single `ValidationActionFilter` runs them and returns a consistent `ApiErrorDto` on failure.
- **Naming and style:** PascalCase for public API, async/await for I/O, dependency injection for testability. Health checks, rate limiting, and global exception handling are configured in `Program.cs` with clear sections.

### Frontend (Vue)

- **Feature-based layout:** `src/features/` (auth, dashboard, tasks, analytics) with views and components per feature. Shared UI in `components/ui/` and `components/layout/`. Desktop/tablet/mobile variants live under `desktop/`, `tablet/`, `mobile/` where the app needs breakpoint-specific UIs.
- **Single API boundary:** All backend calls go through `src/services/api.ts`. Types live in `src/types/`; Pinia stores consume the API and expose state to views. Composables (e.g. `useBoardRealtime`, `useViewport`) encapsulate reusable logic.
- **Consistent patterns:** `<script setup lang="ts">`, typed props/emits, scoped CSS, and design tokens (`var(--bg-primary)`, etc.) for theming and consistency.

### Tests

- **Backend:** Unit tests for validators and filters; controller and integration tests under `Backend/tests/`. EF Core InMemory/SQL test projects as needed.
- **Frontend:** Vitest for unit tests (components, stores, utils); Playwright for E2E. Tests colocated (e.g. `__tests__/Component.spec.ts`) or in `e2e/`.

---

## 2. Thoughtful Architectural Decisions

| Decision | Rationale |
|----------|-----------|
| **Monorepo (Backend + Frontend + Realtime)** | Single repo for aligned releases, shared tooling (e.g. solution at root), and simpler CI (one place for workflows). |
| **REST API + shared DTOs** | Clear contract via `TaskAgent.Contracts`; Swagger/OpenAPI for discovery. REST fits task/project/sprint CRUD and is easy to consume from Vue. |
| **Realtime via separate Socket.IO server** | Keeps the .NET API stateless; realtime server is a small Node service the API POSTs to for broadcasts. Frontend connects to it only when viewing the board. |
| **Pinia + single API module** | One place for HTTP and error handling; stores hold server state and derived data. Avoids scattered `fetch` and keeps a single source of truth per domain. |
| **Validation at API boundary** | FluentValidation on request models ensures invalid data is rejected with consistent error payloads before hitting services. |
| **Correlation ID end-to-end** | Middleware assigns/reads `X-Correlation-Id`; frontend sends and stores it. Supports tracing the same request across logs and UI when debugging. |

---

## 3. Trade-Offs Considered

| Area | Choice | Trade-off |
|------|--------|-----------|
| **Data store** | SQL Server (prod/staging) with optional InMemory for dev | InMemory allows backend to run without Docker/DB; trade-off is dev data not matching production schema/migrations exactly. |
| **Realtime** | Optional Socket.IO server; board works without it | Simpler deployment and no required WebSocket infra; realtime is an enhancement, not a hard dependency. |
| **Auth** | Email/password with hashed passwords; no JWT in this slice | Keeps demo/simple flows straightforward; production could add JWT/OAuth and the same API surface can be extended. |
| **API errors** | Single `ApiErrorDto(message)` for 4xx/5xx | Simple for the client to parse and display; more complex APIs might add codes or multiple messages. |
| **Rate limiting** | Global fixed window per IP (e.g. 100/min) | Good enough for many deployments; high-scale or per-user limits would need a different strategy. |

### Monorepo vs polyrepo (or multi-repo)

| Approach | Pros | Cons |
|----------|------|------|
| **Monorepo** (chosen) | Single source of truth; aligned releases and versioning; easier cross-cutting refactors; one CI/CD pipeline; shared tooling and config; atomic commits across frontend and backend. | Repo size grows; need discipline (path filters, ownership); tooling (e.g. Nx, Turborepo) helps but adds complexity for very large teams. |
We chose a monorepo because the backend, frontend, and realtime server are tightly coupled for this app, and a small team benefits from a single place to change contracts and deploy together.

### SQL vs NoSQL databases

| Factor | SQL (chosen) | NoSQL (e.g. MongoDB, Cosmos DB) |
|--------|--------------|----------------------------------|
| **Schema** | Fixed schema with migrations; referential integrity via foreign keys. | Schemaless or flexible schema; good for evolving or heterogeneous data. |
| **Queries** | Strong support for joins, aggregations, and complex filters (e.g. sprint burndown, cross-project task lists). | Document stores favor denormalization; joins are rare or done in application code. |
| **Transactions** | ACID transactions for multi-table updates (e.g. task + comment in one tx). | Varies; many NoSQL stores offer limited or eventual-consistency transactions. |
| **Use case fit** | TaskAgent’s domain (projects, sprints, tasks, comments, users) is highly relational with clear foreign-key relationships. | Better suited for event logs, feeds, or highly variable document shapes. |

We chose SQL Server because the domain is naturally relational, we need joins and aggregations for analytics and lists, and EF Core migrations give us controlled schema evolution. NoSQL would add complexity without clear benefit for this workload.

---

## 4. Good Communication Between Frontend and Backend

- **Contract alignment:** Backend exposes DTOs (e.g. `ProjectDto`, `TaskItemDto`) and request models in `TaskAgent.Contracts`. Frontend types in `src/types/` and normalizers in `api.ts` (e.g. `normalizeProject`, `normalizeTask`) map JSON to those types and handle dates/optionals consistently.
- **Error handling:** API returns JSON with a `message` field (and optional `code`) on validation and server errors. Frontend `getErrorMessage()` in `api.ts` reads `res.json().message` and surfaces it to the UI (e.g. login/register, notifications). Same shape for rate limit (429) and validation (400).
- **Correlation ID:** Backend `CorrelationIdMiddleware` sets `X-Correlation-Id` on responses; frontend sends it on requests and stores the one from the response. Supports debugging and support ("use this ID to find the request in logs").
- **CORS:** Backend allows configurable origins via `Cors:AllowedOrigins` (and localhost by default). Frontend deployed to GitHub Pages sets `VITE_API_BASE` to the backend URL; backend must list the Pages origin in CORS.
- **Realtime:** API calls the realtime server (e.g. POST `/broadcast`) on task create/update/delete; frontend connects to the same server with `VITE_REALTIME_URL` and joins rooms by `projectId`/`sprintId`. Protocol (events like `TaskCreated`, `TaskUpdated`) is agreed between API, realtime server, and frontend.

---

## 5. Production-Ready Features and Considerations

| Feature | Implementation |
|---------|----------------|
| **Health checks** | `/health/live` (liveness), `/health/ready` (readiness with DB check). Used by Azure Container Apps; excluded from rate limiting. |
| **Rate limiting** | Fixed-window limiter per IP; 429 with `ApiErrorDto` when exceeded. |
| **Global exception handling** | Unhandled exceptions return 500 and a consistent JSON body; correlation ID and message (sanitized in non-dev) logged. |
| **Request logging** | Middleware logs request path and correlation ID for traceability. |
| **Validation** | All relevant request bodies validated with FluentValidation; invalid requests never reach services. |
| **Security** | Passwords hashed (no plaintext); CORS restricted to allowed origins; HTTPS in production. Azure: RBAC/passwordless SQL option. |
| **Secrets** | Connection strings and credentials via environment variables (or Azure Key Vault); no secrets in repo. |
| **Error boundary (frontend)** | Top-level `onErrorCaptured` in `App.vue` shows a fallback UI and logs errors; avoids blank screen on unhandled component errors. |

---