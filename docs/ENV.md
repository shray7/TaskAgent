# Environment variables

Single reference for environment variables used by the TaskAgent monorepo.

## Backend (TaskAgent.Api)

Configure via `appsettings.json`, `appsettings.{Environment}.json`, or environment variables (e.g. `ConnectionStrings__SqlDb`).

| Variable | Description | Example |
|----------|-------------|---------|
| `ConnectionStrings__SqlDb` | SQL Server connection string. | `Server=localhost;Database=TaskAgent;User Id=sa;Password=...;TrustServerCertificate=True` |
| `Cors__AllowedOrigins` | Allowed CORS origins (semicolon- or comma-separated). Localhost is always allowed. | `https://youruser.github.io` |
| `Realtime__ServerUrl` | Base URL of the Socket.IO realtime server. If empty, real-time board updates are disabled. | `http://localhost:3001` |
| `REALTIME_SERVER_URL` | Docker Compose only: passed through to `Realtime__ServerUrl`. Unset = disabled (default). Set to `http://realtime:3001` when using `--profile realtime`. | `http://realtime:3001` |
| `Jwt__Key` | Secret key for signing JWT tokens (min 32 characters). Set via env in production; never commit real secrets. | (use env var or Key Vault) |
| `Jwt__Issuer` | JWT issuer claim. | `TaskAgent.Api` |
| `Jwt__Audience` | JWT audience claim. | `TaskAgent.App` |
| `Jwt__ExpirationMinutes` | Token lifetime in minutes. | `1440` (24h) |

## Frontend (Vue)

Set in `.env` or `.env.local`. For production builds (e.g. GitHub Pages), use repository variables (e.g. `VITE_API_BASE`).

| Variable | Description | Example |
|----------|-------------|---------|
| `VITE_API_BASE` | Backend API base URL. If unset or `mock`, the app uses in-memory mock data. | `http://localhost:5001` |
| `VITE_REALTIME_ENABLED` | Feature flag for realtime board updates. Default: `false`. Set to `true` to enable (requires `VITE_REALTIME_URL`). | `true` |
| `VITE_REALTIME_URL` | Socket.IO realtime server URL for live board updates. Used only when `VITE_REALTIME_ENABLED=true`. | `http://localhost:3001` |

## Realtime server (Backend/realtime)

| Variable | Description | Example |
|----------|-------------|---------|
| `PORT` | HTTP port the server listens on. | `3001` (default) |

## GitHub Actions (Synthetic Tests)

Set in **Settings → Secrets and variables → Actions → Variables**.

| Variable | Description | Example |
|----------|-------------|---------|
| `STAGING_API_URL` | Staging API base URL for health checks. | `https://taskagent-api-staging.<env-id>.eastus.azurecontainerapps.io` |
| `PRODUCTION_API_URL` | Production API base URL for health checks. | `https://taskagent-api.<env-id>.eastus.azurecontainerapps.io` |

Get the FQDN: `az containerapp show --name taskagent-api-staging --resource-group rg-taskagent --query "properties.configuration.ingress.fqdn" -o tsv` (use `taskagent-api` for production).

## Docker Compose toggle (realtime off by default)

Realtime is **disabled by default** when running via `docker compose up api`. To enable:

1. Create `.env` at repo root with `REALTIME_SERVER_URL=http://realtime:3001`
2. Run: `docker compose --profile realtime up --build`
3. Set `VITE_REALTIME_ENABLED=true` and `VITE_REALTIME_URL=http://localhost:3001` in `Frontend/.env`
