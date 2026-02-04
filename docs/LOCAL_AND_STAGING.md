# Local and staging checklist

Use this to make sure the app works **locally** and in **staging** (Azure Container Apps + GitHub Pages) with JWT auth.

---

## Local

### Backend

**Option A: Docker (API uses `appsettings.Development.json`)**

From repo root:
```bash
docker compose up --build api
```
The API runs with `ASPNETCORE_ENVIRONMENT=Development`, so it loads `appsettings.Development.json` (JWT, Logging, InMemory DB). Realtime is **off by default** in Docker; set `REALTIME_SERVER_URL=http://realtime:3001` in `.env` and run with `--profile realtime` to enable.

**Option B: Run locally**

```bash
dotnet run --project Backend/src/TaskAgent.Api
```
Default environment is **Development**; it loads `appsettings.Development.json`, which has:
- `Jwt:Key` – dev secret so login/register issue and validate tokens.
- `DataStore:Type` = **InMemory** – no SQL Server or Docker required.

No extra env vars needed for JWT or DB locally. To use SQL Server locally instead, set `DataStore__Type=Sql` and `ConnectionStrings__SqlDb` (see [docs/ENV.md](ENV.md)).

### Frontend (not in mock mode)

Run the UI locally. Create **`Frontend/.env`** (or `.env.local`) with:
```env
VITE_API_BASE=http://localhost:5001
```
(Use 5001 when the API runs in Docker; or the port from `launchSettings.json` if you run the API locally.)

Then:
```bash
cd Frontend && npm install && npm run dev
```
With `VITE_API_BASE` set, the UI uses the real API (not mock mode). Log in – the API returns a JWT and the frontend sends it on every request.

### Realtime (optional)

- **Docker:** Realtime is off by default. To enable: add `REALTIME_SERVER_URL=http://realtime:3001` to repo root `.env`, then `docker compose --profile realtime up --build`.
- **Standalone:** `Realtime:ServerUrl` in appsettings.Development.json; run `cd Backend/realtime && npm run dev`.
- Frontend: add `VITE_REALTIME_ENABLED=true` and `VITE_REALTIME_URL=http://localhost:3001` to `Frontend/.env`.

---

## Staging

### Backend (Azure Container App – staging)

The staging container app must have these set (Azure Portal → your Container App → **Contained apps** → **taskagent-api-staging** → **Environment variables**, or via CLI/ARM):

| Variable | Required | Description |
|----------|----------|-------------|
| `Jwt__Key` | **Yes** | Secret key for signing/validating JWTs (min 32 characters). Generate a strong value; store in GitHub Actions **Secrets** or Azure Key Vault and inject into the container. **Never commit this value.** |
| `ASPNETCORE_ENVIRONMENT` | Recommended | Set to `Staging` so the app loads `appsettings.Staging.json` (e.g. `Cors:AllowedOrigins` for your Pages URL). |
| `ConnectionStrings__SqlDb` | **Yes** | Staging SQL connection string (often from GitHub env secret `STAGING_SQL_CONNECTION_STRING`; deploy workflow uses it for migrations – the running app needs the same via this env). |
| `Cors__AllowedOrigins` | If not in appsettings.Staging | Your frontend origin, e.g. `https://YOUR_USERNAME.github.io`. Already in `appsettings.Staging.json` if env is `Staging`. |

Optional: `Jwt__ExpirationMinutes`, `Realtime__ServerUrl`, etc. (see [ENV.md](ENV.md)).

**How to set `Jwt__Key` for staging (and production)**

- **Recommended – Azure Key Vault:** From repo root, run:
  ```bash
  ./scripts/setup-jwt-keyvault.sh
  ```
  This generates a secure key, stores it in Key Vault as `TaskAgent-JwtKey`, and configures both Container Apps to use it via Key Vault reference (`Jwt__Key=secretref:jwt-key`). Optional: set `CORS_ALLOWED_ORIGINS` if needed (e.g. `CORS_ALLOWED_ORIGINS="https://youruser.github.io" ./scripts/setup-jwt-keyvault.sh`). See [infra/README.md](../infra/README.md#jwt-signing-key-for-auth).
- **Option B – Azure Portal:** In the Container App, add environment variable `Jwt__Key` and paste a generated secret (or add a secret that references Key Vault, then set `Jwt__Key=secretref:secret-name`).
- **Option C – Azure CLI (literal value):**  
  `az containerapp update --name taskagent-api-staging --resource-group <rg> --set-env-vars "Jwt__Key=<your-secret>"`

If `Jwt__Key` is missing or wrong, login/register will fail or token validation will return 401.

### Frontend (GitHub Pages)

1. **Point the built app at staging API**  
   The deploy-pages workflow uses **Variables** → `VITE_API_BASE` (or a fallback). Set:
   - **Settings** → **Secrets and variables** → **Actions** → **Variables**
   - Add (or edit) `VITE_API_BASE` = your staging API URL, e.g.  
     `https://taskagent-api-staging.<random>.eastus.azurecontainerapps.io`  
   So the built SPA calls staging and gets a JWT from staging on login.

2. **CORS**  
   Staging backend must allow your Pages origin. If `appsettings.Staging.json` has `Cors:AllowedOrigins: "https://shray7.github.io"` (or your Pages URL) and the container runs with `ASPNETCORE_ENVIRONMENT=Staging`, you’re set. Otherwise set `Cors__AllowedOrigins` on the container app.

---

## Quick verification

- **Local:** Open the app at the dev URL, log in → dashboard and API calls should work. Check browser Network tab for `Authorization: Bearer …` on requests after login.
- **Staging:** Open `https://YOUR_USERNAME.github.io/TaskAgent/` (or your Pages URL), log in → same. If you get 401 on protected routes, the staging backend is likely missing or misconfigured `Jwt__Key`, or the frontend is not sending the token (check `VITE_API_BASE` and that you’re logged in).

Tokens are **per environment**: local login gives a local token; staging login gives a staging token. They are not interchangeable.
