# TaskAgent

Monorepo containing Backend API, File Service, and Frontend.

## Structure

```
TaskAgent/
├── Backend/       # .NET API (tasks, projects, sprints, comments)
├── Frontend/      # Vue.js SPA
├── FileService/   # Standalone file storage (local or Azure Blob)
└── docker-compose.yml
```

## Run with Docker

```bash
docker compose up --build
```

- **API**: http://localhost:5001
- **File Service**: http://localhost:5002
- **Web (nginx)**: http://localhost:80 – serves the UI and proxies `/api` to the API and `/api/files` to File Service

## Run locally (development)

**Backend:**
```bash
cd Backend && dotnet run --project src/TaskAgent.Api
```

**File Service:**
```bash
cd FileService && dotnet run
```

**Frontend:**
```bash
cd Frontend && npm install && npm run dev
```

Set `VITE_API_BASE=http://localhost:5001` in Frontend `.env` for local API. File uploads go to File Service at port 5002 when using `VITE_FILE_SERVICE_URL` (or configure proxy).
