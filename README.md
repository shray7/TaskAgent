# TaskAgent

**TaskAgent** is a task management system for projects and sprints. Manage tasks in list, board, or analytics views with a responsive UI across desktop, tablet, and mobile.

## Features

| Feature | Description |
|---------|-------------|
| **Authentication** | Login, sign up, demo accounts |
| **Dashboard** | List view (table), Board view (Kanban columns), Analytics view (sprint progress, burndown, funnel, tag breakdown, workload) |
| **My Tasks** | Cross-project task view with filters (assignee, priority, project), stats |
| **Projects & Sprints** | CRUD for projects and sprints, project/sprint selectors |
| **Tasks** | Create/edit tasks, inline editing (title, assignee, due date, priority), drag-and-drop on board, tags, comments |
| **UI** | Dark/light theme, responsive layouts for desktop, tablet, mobile |
| **Real-time board** | Socket.IO server (`Backend/realtime`); task create/update/delete broadcast so multiple viewers see changes immediately |

## Technologies

**Frontend**
- Vue 3, TypeScript, Vite 7
- Pinia (state), Vue Router, pinia-plugin-persistedstate
- Lucide Vue (icons)
- Vitest, Playwright (tests)

**Backend**
- ASP.NET Core 9, Entity Framework Core 9
- SQL Server (prod/staging) or InMemory (dev)
- Swagger/OpenAPI
- Azure AD / RBAC for passwordless SQL (production)

**Infrastructure**
- Docker, docker-compose (from repo root)
- GitHub Actions: `build-backend.yml`, `build-frontend.yml` (PR builds), `ci.yml` (full CI), `deploy.yml` (backend to Azure), `deploy-pages.yml` (frontend to GitHub Pages), `synthetic-tests.yml`
- Azure: Container Apps, ACR, Azure SQL
- GitHub Pages (frontend hosting)

## Architecture (local development)

```mermaid
flowchart LR
    subgraph local [Local]
        Browser[Browser]
        Vue[Vue dev server]
        API[TaskAgent API :5001]
        Realtime[Socket.IO :3001]
        Store[(InMemory / SQL)]
    end

    Browser --> Vue
    Vue -->|REST| API
    Vue -.->|optional| Realtime
    API --> Store
    API -.->|broadcasts| Realtime
```

## Project Structure

```
TaskAgent/
├── Backend/             # .NET API (tasks, projects, sprints, comments)
│   ├── src/
│   │   ├── TaskAgent.Api/
│   │   ├── TaskAgent.Contracts/
│   │   └── TaskAgent.DataAccess/
│   ├── realtime/        # Socket.IO server for live board updates
│   ├── postman/         # Postman collection for API
│   ├── spec.md          # API spec
│   └── tests/
├── Frontend/            # Vue.js SPA
│   ├── src/
│   │   ├── features/    # auth, dashboard, tasks, analytics
│   │   ├── components/
│   │   └── stores/
│   ├── e2e/             # Playwright tests
│   └── screenshots/     # Generated screenshots (npm run screenshots)
├── docs/                # ENV.md, LOCAL_AND_STAGING.md
├── infra/               # Azure setup, RBAC scripts
├── scripts/             # GitHub secrets, JWT Key Vault setup
├── .github/workflows/   # build-backend, build-frontend, ci, deploy, deploy-pages, synthetic-tests
├── TaskAgent.sln        # Solution at repo root
└── docker-compose.yml
```

## Quick Start

### Run with Docker

From repo root:

```bash
docker compose up --build
```

- **API**: http://localhost:5001 (InMemory DB; use `--profile databases` to add SQL Server / MongoDB if needed).
- API image is built from repo root (`context: .`, `dockerfile: Backend/Dockerfile`) so `TaskAgent.sln` and Backend code are available (same as in CI).

### Run locally (development)

**New to the repo?** For clone-and-run plus **working login**, see [CONTRIBUTING.md – First-time setup](CONTRIBUTING.md#first-time-setup-clone-and-run-with-login) (create `Frontend/.env` from `.env.example`, run backend then frontend).

Commands are run from repo root where noted.

**Backend:**
```bash
dotnet run --project Backend/src/TaskAgent.Api
```

**Frontend:**
```bash
cd Frontend && npm install && npm run dev
```

**Realtime (optional, for live board updates):**
```bash
cd Backend/realtime && npm install && npm run dev
```
Set `Realtime__ServerUrl` in backend config and `VITE_REALTIME_URL` in Frontend `.env` (see [docs/ENV.md](docs/ENV.md)).

Set `VITE_API_BASE=http://localhost:5001` in Frontend `.env` for local API. For more detail and a full env reference, see [CONTRIBUTING.md](CONTRIBUTING.md) and [docs/ENV.md](docs/ENV.md).

## Scripts

**Frontend**
- `npm run dev` – Dev server
- `npm run build` – Production build
- `npm run preview` – Preview production build
- `npm run screenshots` – Generate Playwright screenshots (desktop, tablet, mobile)
- `npm run test:unit` – Vitest
- `npm run test:e2e` – Playwright E2E
- `npm run lint` – Lint (oxlint + ESLint)
- `npm run format` – Format with Prettier

**Backend**
- From repo root: `dotnet run --project Backend/src/TaskAgent.Api` – Run API
- From repo root: `dotnet test TaskAgent.sln` – Run tests

**Realtime**
- `cd Backend/realtime && npm run dev` – Socket.IO server (default port 3001)

## Environment variables

See [docs/ENV.md](docs/ENV.md) for a single reference of all environment variables used by the backend, frontend, and realtime server.

**Backend (appsettings or env)**  
`ConnectionStrings__SqlDb`, `Cors__AllowedOrigins`, `Realtime__ServerUrl`, `Jwt__Key` (required for auth in staging/production)

**Frontend (`.env` or build-time)**  
`VITE_API_BASE`, `VITE_REALTIME_URL` (optional)

**Local and staging setup:** For a step-by-step checklist so the app works both locally and in staging (JWT, CORS, env vars), see [docs/LOCAL_AND_STAGING.md](docs/LOCAL_AND_STAGING.md).

## Deployment

### Backend

Deployed to Azure Container Apps via `.github/workflows/deploy.yml` (staging then production). Uses Azure AD / RBAC for passwordless SQL access.

### Frontend (GitHub Pages)

Deployed via `.github/workflows/deploy-pages.yml`.

1. **Log in to GitHub** (one-time): `gh auth login -h github.com -p https -w`
2. **Create repo and push**: `gh repo create TaskAgent --public --source=. --push --description "TaskAgent - Vue.js task management with .NET backend"`
3. **Enable Pages**: Repo → **Settings** → **Pages** → **Source**: **GitHub Actions**
4. **API URL**: Settings → Secrets and variables → Actions → Variables → `VITE_API_BASE` = your staging backend URL (e.g. `https://taskagent-api-staging....azurecontainerapps.io`) so the built app calls your API.
5. **Backend staging**: Set `Jwt__Key` (32+ character secret) on the staging Container App so login/register work. Set `Cors__AllowedOrigins` (or use `appsettings.Staging.json`) to your Pages URL. See [docs/LOCAL_AND_STAGING.md](docs/LOCAL_AND_STAGING.md).

Workflow runs on push to `main`. App will be at **https://YOUR_USERNAME.github.io/TaskAgent/**.

---

For an evaluation-focused overview (architecture, trade-offs, frontend–backend communication, production considerations), see [Evaluation Guide](EVALUATION_GUIDE.md).
