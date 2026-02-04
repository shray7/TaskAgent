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
| **Frontend hosting** | GitHub Pages (static) | Low cost and simple CI; trade-off is no server-side auth or dynamic env at runtime—config via build-time vars (e.g. `VITE_API_BASE`). |
| **Auth** | Email/password with hashed passwords; no JWT in this slice | Keeps demo/simple flows straightforward; production could add JWT/OAuth and the same API surface can be extended. |
| **File attachments** | Removed from this repo (previously separate File Service) | Keeps the monorepo focused on core task management; file uploads can be reintroduced as a separate service or extension. |
| **API errors** | Single `ApiErrorDto(message)` for 4xx/5xx | Simple for the client to parse and display; more complex APIs might add codes or multiple messages. |
| **Rate limiting** | Global fixed window per IP (e.g. 100/min) | Good enough for many deployments; high-scale or per-user limits would need a different strategy. |

---

## 4. Good Communication Between Frontend and Backend

- **Contract alignment:** Backend exposes DTOs (e.g. `ProjectDto`, `TaskItemDto`) and request models in `TaskAgent.Contracts`. Frontend types in `src/types/` and normalizers in `api.ts` (e.g. `normalizeProject`, `normalizeTask`) map JSON to those types and handle dates/optionals consistently.
- **Error handling:** API returns JSON with a `message` field (and optional `code`) on validation and server errors. Frontend `getErrorMessage()` in `api.ts` reads `res.json().message` and surfaces it to the UI (e.g. login/register, notifications). Same shape for rate limit (429) and validation (400).
- **Correlation ID:** Backend `CorrelationIdMiddleware` sets `X-Correlation-Id` on responses; frontend sends it on requests and stores the one from the response. Supports debugging and support (“use this ID to find the request in logs”).
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
| **Deployment** | Backend: Docker image built from repo root, pushed to ACR, deployed to Azure Container Apps (staging → production) with EF migrations. Frontend: built with Vite, deployed to GitHub Pages. |
| **Error boundary (frontend)** | Top-level `onErrorCaptured` in `App.vue` shows a fallback UI and logs errors; avoids blank screen on unhandled component errors. |
| **Notifications (frontend)** | Central notification store and `AppNotifications` component for API/validation error messages. |

---

## 6. Clear Documentation and Setup Instructions

| Doc | Purpose |
|-----|---------|
| **README.md** | High-level overview, features, tech stack, architecture diagrams, project structure, quick start (Docker + local), scripts, env summary, deployment (backend + Pages). |
| **CONTRIBUTING.md** | How to run backend, frontend, and realtime locally; how to run tests; pointer to full env reference. |
| **docs/ENV.md** | Single reference for all env vars: backend (e.g. `ConnectionStrings__SqlDb`, `Cors__AllowedOrigins`, `Realtime__ServerUrl`), frontend (`VITE_API_BASE`, `VITE_REALTIME_URL`), realtime server (`PORT`). |
| **Backend/realtime/README.md** | What the Socket.IO server does, how to run it, and how to configure the API and frontend to use it. |
| **.github/workflows/** | `build-backend.yml` / `build-frontend.yml` for PR builds; `ci.yml` for full CI; `deploy.yml` for backend (build, test, push image, migrate, deploy staging → production); `deploy-pages.yml` for frontend. Path filters so only relevant changes trigger each workflow. |

### Quick setup (recap)

- **Backend (local):** From repo root: `dotnet run --project Backend/src/TaskAgent.Api`. Optionally set `ConnectionStrings__SqlDb` and `Realtime__ServerUrl` (see [docs/ENV.md](docs/ENV.md)).
- **Frontend (local):** `cd Frontend && npm install && npm run dev`. Set `VITE_API_BASE=http://localhost:5001` (and optionally `VITE_REALTIME_URL`) in `.env`.
- **Realtime (optional):** `cd Backend/realtime && npm install && npm run dev` (default port 3001).
- **Docker:** From repo root, `docker compose up --build` for API (and optionally `--profile databases` for SQL Server/MongoDB). See [README.md](README.md).
- **Tests:** From repo root: `dotnet test TaskAgent.sln`. Frontend: `cd Frontend && npm run test:unit` and `npm run test:e2e`.

This structure and documentation are intended to make the codebase easy to navigate, reason about, and run locally or in production.
