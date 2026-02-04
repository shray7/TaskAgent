# TaskAgent Realtime (Socket.IO)

Socket.IO server for real-time board updates. When multiple users view the same project/sprint board, task create/update/delete from the .NET API are broadcast so all clients see changes immediately.

## Run locally

```bash
npm install
npm run dev
```

Runs on port **3001** by default (`PORT` env to override).

## Configuration

- **.NET API**: Set `Realtime:ServerUrl` (e.g. `http://localhost:3001`) in `appsettings.Development.json` so the API POSTs to this server on task changes.
- **Vue**: Set `VITE_REALTIME_ENABLED=true` and `VITE_REALTIME_URL` (e.g. `http://localhost:3001`) so the app connects to this server when viewing the dashboard.

If either URL is unset, real-time is disabled (board still works without live updates).

## Protocol

- Clients connect and emit `join-board` with `{ projectId, sprintId? }` to join room `board:{projectId}:{sprintId ?? 'all'}`.
- .NET API POSTs to `/broadcast` with `{ projectId, sprintId?, event, data }`; this server emits `event` with `data` to that room.
- Events: `TaskCreated`, `TaskUpdated`, `TaskDeleted`.
