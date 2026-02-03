# Contributing

## Running locally

1. **Backend**
   ```bash
   cd Backend && dotnet run --project src/TaskAgent.Api
   ```
   See [docs/ENV.md](docs/ENV.md) for `ConnectionStrings__SqlDb`, `Realtime__ServerUrl`, etc.

2. **Frontend**
   ```bash
   cd Frontend && npm install && npm run dev
   ```
   Set `VITE_API_BASE` in `.env` to your backend URL (e.g. `http://localhost:5001`).

3. **Realtime (optional)**  
   For live board updates: `cd Backend/realtime && npm install && npm run dev`. Set `Realtime__ServerUrl` in backend config and `VITE_REALTIME_URL` in frontend `.env`.

## Tests

- **Backend:** From repo root: `dotnet test TaskAgent.sln`
- **Frontend unit:** `cd Frontend && npm run test:unit`
- **Frontend E2E:** `cd Frontend && npm run test:e2e`

## Environment variables

See [docs/ENV.md](docs/ENV.md) for a full list of backend, frontend, and realtime environment variables.
