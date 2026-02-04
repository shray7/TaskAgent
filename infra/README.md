# Azure Infrastructure Setup

## RBAC (No Connection String Secrets)

Database access uses **managed identity** and **Microsoft Entra authentication**—no connection strings with passwords are stored. Container Apps use `DefaultAzureCredential` to obtain tokens automatically.

## Prerequisites

- Azure CLI installed (`az --version`)
- Logged into Azure (`az login`)
- Target subscription selected (`az account set --subscription "<id>"`)
- GitHub repository (Backend root = repo root, with `TaskAgent.sln`, `src/`, `Dockerfile`)
- **sqlcmd** (optional, for automated DB user creation): `brew install sqlcmd` on macOS, or run SQL manually

## Resources Created

| Resource | Name | Purpose |
|----------|------|---------|
| Resource Group | rg-taskagent | Container for all resources |
| Container Registry | taskagentacr | Docker image storage |
| Container Apps | taskagent-api, taskagent-api-staging | API hosting |
| SQL Database | taskagent-sql-eus2 (DB: taskagent) | Primary database |
| Key Vault | taskagentflow-kv | Secrets (storage, Redis—no SQL) |
| Redis Cache | taskagent-redis | Caching layer (optional) |
| Storage Account | taskagentst | File storage (blobs) |

## Quick Setup

```bash
# From Backend directory
chmod +x infra/setup-azure.sh
./infra/setup-azure.sh
```

The script will:
- Create all Azure resources
- Set the current user as Microsoft Entra admin on the SQL server
- Create DB users for Container App managed identities and the GitHub Actions service principal
- Configure Container Apps with `DataStore__SqlServer` and `DataStore__SqlDatabase` (no connection string secrets)

If `sqlcmd` is not installed (or Entra auth fails), run **`infra/create-db-users-rbac.sql`** manually in Azure Portal → SQL Database → Query Editor, authenticating as the SQL Entra Admin. The script uses `OBJECT_ID` for reliable identity resolution.

**GitHub Pages CORS:** To allow the Vue frontend hosted on GitHub Pages to call this API, run the setup with:
```bash
CORS_ALLOWED_ORIGINS="https://YOUR_USERNAME.github.io;https://YOUR_USERNAME.github.io/YOUR_REPO" ./infra/setup-azure.sh
```
Or add `Cors__AllowedOrigins` manually in Container App → Environment variables.

## GitHub Configuration

### 1. Repository Secrets

| Secret | Value |
|--------|-------|
| `AZURE_CREDENTIALS` | JSON from setup script (service principal for `az login`) |
| `STAGING_SQL_CONNECTION_STRING` | **Passwordless** connection string for EF migrations (staging) |
| `PRODUCTION_SQL_CONNECTION_STRING` | **Passwordless** connection string for EF migrations (production) |

**Passwordless connection string format** (no password; uses `DefaultAzureCredential` after `az login`):

```
Server=tcp:taskagent-sql-eus2.database.windows.net,1433;Database=taskagent;Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

Use the same value for both staging and production if you have a single database. Change `Database=` if you use separate databases.

### 2. Environments

Create environments (Settings → Environments):

- **staging** – deploys automatically after migrations
- **production** – optionally add required reviewers for manual approval

### 3. (Optional) OIDC for Passwordless Azure Login

To avoid storing a service principal client secret, configure [GitHub OIDC](https://learn.microsoft.com/en-us/azure/developer/github/connect-from-azure) with a federated credential. Then use `client-id`, `tenant-id`, and `subscription-id` instead of `AZURE_CREDENTIALS`.

## Pipeline Flow

```
Push to main
    │
    ▼
Build & Test
    │
    ▼
Build Image → Push to ACR
    │
    ▼
Login to Azure → Run EF migrations (staging)
    │
    ▼
Deploy to Staging
    │
    ▼
Login to Azure → Run EF migrations (production)
    │
    ▼
Deploy to Production → Health check
```

## Manual Deployment

```bash
# From Backend directory - build and push
az acr build --registry taskagentacr --image taskagent:manual .

# Deploy to staging
az containerapp update --name taskagent-api-staging --resource-group rg-taskagent \
  --image taskagentacr.azurecr.io/taskagent:manual

# Deploy to production
az containerapp update --name taskagent-api --resource-group rg-taskagent \
  --image taskagentacr.azurecr.io/taskagent:manual
```

## Key Vault

Secrets stored: `redis-connection-string`, `azure-storage-connection-string`, `TaskAgent-JwtKey` (no SQL connection string).

### JWT signing key (for auth)

The API uses a JWT signing key (`Jwt__Key`) so login/register can issue and validate tokens. You can:

1. **Let `setup-azure.sh` create it** – The script generates a key and stores it in Key Vault as `TaskAgent-JwtKey`, then configures both Container Apps to use it via Key Vault reference (env `Jwt__Key=secretref:jwt-key`).

2. **Create or rotate it later** – From repo root:
   ```bash
   chmod +x scripts/setup-jwt-keyvault.sh
   CORS_ALLOWED_ORIGINS="https://youruser.github.io" ./scripts/setup-jwt-keyvault.sh
   ```
   This generates a new key, stores it in Key Vault, and wires both staging and production Container Apps to use it. Optional: set `RESOURCE_GROUP`, `KEYVAULT_NAME`, `APP_NAME` if different from defaults.

## Monitoring

```bash
az containerapp logs show --name taskagent-api --resource-group rg-taskagent --follow
```

Metrics: Azure Portal → Container Apps → taskagent-api → Metrics
