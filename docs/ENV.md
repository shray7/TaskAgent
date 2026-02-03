# Environment variables

Single reference for environment variables used by the TaskAgent monorepo.

## Backend (TaskAgent.Api)

Configure via `appsettings.json`, `appsettings.{Environment}.json`, or environment variables (e.g. `ConnectionStrings__SqlDb`).

| Variable | Description | Example |
|----------|-------------|---------|
| `ConnectionStrings__SqlDb` | SQL Server connection string. | `Server=localhost;Database=TaskAgent;User Id=sa;Password=...;TrustServerCertificate=True` |
| `Cors__AllowedOrigins` | Allowed CORS origins (semicolon- or comma-separated). Localhost is always allowed. | `https://youruser.github.io` |
| `Realtime__ServerUrl` | Base URL of the Socket.IO realtime server. If empty, real-time board updates are disabled. | `http://localhost:3001` |

## Frontend (Vue)

Set in `.env` or `.env.local`. For production builds (e.g. GitHub Pages), use repository variables (e.g. `VITE_API_BASE`).

| Variable | Description | Example |
|----------|-------------|---------|
| `VITE_API_BASE` | Backend API base URL. If unset or `mock`, the app uses in-memory mock data. | `http://localhost:5001` |
| `VITE_REALTIME_URL` | Socket.IO realtime server URL for live board updates. Optional. | `http://localhost:3001` |

## Realtime server (Backend/realtime)

| Variable | Description | Example |
|----------|-------------|---------|
| `PORT` | HTTP port the server listens on. | `3001` (default) |
