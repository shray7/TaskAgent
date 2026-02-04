# Contributing to TaskAgent

Thanks for your interest in contributing. This guide covers running the app locally, running tests, and where to find more detail.

## First-time setup (clone and run with login)

After cloning the repo, do the following so you can run the app and **log in successfully** (real backend + JWT).

1. **Backend (API)**  
   From repo root:
   ```bash
   dotnet run --project Backend/src/TaskAgent.Api
   ```
   - Uses **Development** config: JWT key and **in-memory database** (no SQL Server or Docker required).  
   - API will be at **http://localhost:5001** (or the port in `Backend/src/TaskAgent.Api/Properties/launchSettings.json`).

2. **Frontend**  
   Create env file and install/run:
   ```bash
   cd Frontend
   cp .env.example .env
   # Edit .env if needed: VITE_API_BASE should be http://localhost:5001
   npm install && npm run dev
   ```
   - Without `Frontend/.env` (and `VITE_API_BASE`), the app uses **mock data** and does not call your backend, so login won’t use the real API or JWT.  
   - With `VITE_API_BASE=http://localhost:5001`, the UI talks to your local API; **Register** or **Login** will issue a JWT and you’ll be logged in.

3. **Optional – Realtime**  
   For live board updates: `cd Backend/realtime && npm install && npm run dev`. Set `VITE_REALTIME_URL=http://localhost:3001` in `Frontend/.env`.

You’re set: open the app, register or log in, and use the dashboard.

## Running locally

All commands assume you are at the **repo root** unless otherwise noted.

### 1. Backend

```bash
dotnet run --project Backend/src/TaskAgent.Api
```

- Local dev uses **InMemory** data store (no database required). See [docs/ENV.md](docs/ENV.md) if you want to use SQL Server locally (e.g. `DataStore__Type=Sql`, `ConnectionStrings__SqlDb`).
- API runs at http://localhost:5001 (or the port in `Backend/src/TaskAgent.Api/Properties/launchSettings.json`).

### 2. Frontend

```bash
cd Frontend && npm install && npm run dev
```

- Set `VITE_API_BASE=http://localhost:5001` in `Frontend/.env` (or `.env.local`) so the UI talks to your local API.
- If unset or `mock`, the app uses in-memory mock data.

### 3. Realtime (optional)

For live board updates when multiple users view the same project/sprint:

```bash
cd Backend/realtime && npm install && npm run dev
```

- Server runs on port **3001** by default (`PORT` env to override).
- Backend: set `Realtime__ServerUrl` (e.g. `http://localhost:3001`) in appsettings or env.
- Frontend: set `VITE_REALTIME_ENABLED=true` and `VITE_REALTIME_URL=http://localhost:3001` in `.env`.

See [Backend/realtime/README.md](Backend/realtime/README.md) for protocol and configuration details.

### 4. Docker (optional)

From repo root:

```bash
docker compose up --build
```

- Starts the API only (InMemory). Add `--profile databases` to also run SQL Server and/or MongoDB.
- See [README.md](README.md) for full Docker and env options.

## Tests

- **Backend:** From repo root:
  ```bash
  dotnet test TaskAgent.sln
  ```
- **Frontend unit (Vitest):**
  ```bash
  cd Frontend && npm run test:unit
  ```
- **Frontend E2E (Playwright):**
  ```bash
  cd Frontend && npm run test:e2e
  ```

## Environment variables

A single reference for all variables used by the backend, frontend, and realtime server is in [docs/ENV.md](docs/ENV.md).

## CI and workflows

- **PRs:** `build-backend.yml` and `build-frontend.yml` run on pull requests (path-filtered). `ci.yml` runs full backend build, test, and Docker build on PRs.
- **Deploy:** Backend is deployed via `deploy.yml` (Azure Container Apps); frontend via `deploy-pages.yml` (GitHub Pages). See [README.md](README.md#deployment).

## More documentation

- [README.md](README.md) — Overview, features, quick start, deployment.
- [docs/LOCAL_AND_STAGING.md](docs/LOCAL_AND_STAGING.md) — Checklist so the app works locally and in staging (JWT, CORS, env vars).
- [Evaluation Guide](EVALUATION_GUIDE.md) — Architecture, trade-offs, frontend–backend communication, production considerations.
- [docs/ENV.md](docs/ENV.md) — Environment variable reference.
