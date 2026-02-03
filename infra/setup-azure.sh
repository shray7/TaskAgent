#!/bin/bash
set -e

# Configuration - aligned with deploy.yml (taskagent naming)
RESOURCE_GROUP="rg-taskagent"
LOCATION="eastus"
SQL_LOCATION="eastus2"  # Use different region if East US has SQL provisioning limits
ACR_NAME="taskagentflowacr"
CONTAINER_APP_ENV="taskagent-env"
APP_NAME="taskagent-api"
SQL_SERVER_NAME="taskagent-sql-eus2"
SQL_DB_NAME="taskagent"
SQL_ADMIN_USER="taskagentadmin"
KEYVAULT_NAME="taskagentflow-kv"
REDIS_NAME="taskagent-redis"
STORAGE_ACCOUNT_NAME="taskagentst"
BLOB_CONTAINER_NAME="taskagent-files"
SP_NAME="taskagent-github-actions"

echo "Creating resource group (if not exists)..."
az group create --name $RESOURCE_GROUP --location $LOCATION

echo "Creating Container Registry (if not exists)..."
if ! az acr show --name $ACR_NAME --resource-group $RESOURCE_GROUP &>/dev/null; then
  az acr create \
    --resource-group $RESOURCE_GROUP \
    --name $ACR_NAME \
    --sku Basic \
    --admin-enabled true
else
  echo "  ACR already exists, skipping..."
fi

echo "Creating Container Apps environment (if not exists)..."
if ! az containerapp env show --name $CONTAINER_APP_ENV --resource-group $RESOURCE_GROUP &>/dev/null; then
  az containerapp env create \
    --name $CONTAINER_APP_ENV \
    --resource-group $RESOURCE_GROUP \
    --location $LOCATION
else
  echo "  Container Apps environment already exists, skipping..."
fi

echo "Creating/updating staging Container App..."
if ! az containerapp show --name "${APP_NAME}-staging" --resource-group $RESOURCE_GROUP &>/dev/null; then
  az containerapp create \
    --name "${APP_NAME}-staging" \
    --resource-group $RESOURCE_GROUP \
    --environment $CONTAINER_APP_ENV \
    --image mcr.microsoft.com/dotnet/samples:aspnetapp \
    --target-port 8080 \
    --ingress external \
    --min-replicas 0 \
    --max-replicas 3 \
    --registry-server "${ACR_NAME}.azurecr.io" \
    --registry-identity system
else
  echo "  Staging app exists, updating configuration..."
  az containerapp update \
    --name "${APP_NAME}-staging" \
    --resource-group $RESOURCE_GROUP \
    --min-replicas 0 \
    --max-replicas 3

  az containerapp ingress update \
    --name "${APP_NAME}-staging" \
    --resource-group $RESOURCE_GROUP \
    --target-port 8080
fi

echo "Creating/updating production Container App..."
if ! az containerapp show --name $APP_NAME --resource-group $RESOURCE_GROUP &>/dev/null; then
  az containerapp create \
    --name $APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --environment $CONTAINER_APP_ENV \
    --image mcr.microsoft.com/dotnet/samples:aspnetapp \
    --target-port 8080 \
    --ingress external \
    --min-replicas 1 \
    --max-replicas 10 \
    --registry-server "${ACR_NAME}.azurecr.io" \
    --registry-identity system
else
  echo "  Production app exists, updating configuration..."
  az containerapp update \
    --name $APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --min-replicas 1 \
    --max-replicas 10

  az containerapp ingress update \
    --name $APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --target-port 8080
fi

echo "Creating Azure SQL Server (if not exists)..."
SQL_ADMIN_PASS=""
if ! az sql server show --name $SQL_SERVER_NAME --resource-group $RESOURCE_GROUP &>/dev/null; then
  SQL_ADMIN_PASS=$(openssl rand -base64 24)

  az sql server create \
    --name $SQL_SERVER_NAME \
    --resource-group $RESOURCE_GROUP \
    --location $SQL_LOCATION \
    --admin-user $SQL_ADMIN_USER \
    --admin-password "$SQL_ADMIN_PASS"

  az sql db create \
    --resource-group $RESOURCE_GROUP \
    --server $SQL_SERVER_NAME \
    --name $SQL_DB_NAME \
    --service-objective S0

  az sql server firewall-rule create \
    --resource-group $RESOURCE_GROUP \
    --server $SQL_SERVER_NAME \
    --name AllowAzureServices \
    --start-ip-address 0.0.0.0 \
    --end-ip-address 0.0.0.0

  echo ""
  echo "=========================================="
  echo "NEW SQL SERVER CREATED - SAVE FOR EMERGENCY RECOVERY"
  echo "=========================================="
  echo "SQL Server: $SQL_SERVER_NAME.database.windows.net"
  echo "Database: $SQL_DB_NAME"
  echo "SQL Admin: $SQL_ADMIN_USER (legacy auth - for recovery only)"
  echo "SQL Password: $SQL_ADMIN_PASS"
  echo ""
else
  echo "  SQL Server already exists, skipping..."
fi

echo "Configuring Microsoft Entra (Azure AD) authentication for Azure SQL..."
CURRENT_USER_ID=$(az ad signed-in-user show --query id -o tsv 2>/dev/null || true)
CURRENT_USER_UPN=$(az ad signed-in-user show --query userPrincipalName -o tsv 2>/dev/null || true)
if [ -n "$CURRENT_USER_ID" ]; then
  az sql server ad-admin create \
    --resource-group $RESOURCE_GROUP \
    --server-name $SQL_SERVER_NAME \
    --display-name "SQL-Entra-Admin" \
    --object-id "$CURRENT_USER_ID" 2>/dev/null || \
  az sql server ad-admin update \
    --resource-group $RESOURCE_GROUP \
    --server-name $SQL_SERVER_NAME \
    --display-name "SQL-Entra-Admin" \
    --object-id "$CURRENT_USER_ID" 2>/dev/null || true
  echo "  Set current user as Entra admin for SQL server"
fi

# Add firewall rule for current IP to run CREATE USER (if not Cloud Shell)
CURRENT_IP=$(curl -s -4 https://api.ipify.org 2>/dev/null || true)
if [ -n "$CURRENT_IP" ] && [ "$CURRENT_IP" != "127.0.0.1" ]; then
  az sql server firewall-rule create \
    --resource-group $RESOURCE_GROUP \
    --server $SQL_SERVER_NAME \
    --name "SetupScriptClient" \
    --start-ip-address "$CURRENT_IP" \
    --end-ip-address "$CURRENT_IP" 2>/dev/null || true
fi

echo "Creating/updating Azure Key Vault..."
if ! az keyvault show --name $KEYVAULT_NAME --resource-group $RESOURCE_GROUP &>/dev/null; then
  az keyvault create \
    --name $KEYVAULT_NAME \
    --resource-group $RESOURCE_GROUP \
    --location $LOCATION \
    --enable-rbac-authorization true

  CURRENT_USER=$(az ad signed-in-user show --query id -o tsv 2>/dev/null || true)
  if [ -n "$CURRENT_USER" ]; then
    az role assignment create \
      --role "Key Vault Administrator" \
      --assignee $CURRENT_USER \
      --scope /subscriptions/$(az account show --query id -o tsv)/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.KeyVault/vaults/$KEYVAULT_NAME
  fi
  echo "  Key Vault created: $KEYVAULT_NAME"
else
  echo "  Key Vault already exists, skipping..."
fi

echo "Creating Azure Storage Account (if not exists)..."
if ! az storage account show --name $STORAGE_ACCOUNT_NAME --resource-group $RESOURCE_GROUP &>/dev/null; then
  az storage account create \
    --name $STORAGE_ACCOUNT_NAME \
    --resource-group $RESOURCE_GROUP \
    --location $LOCATION \
    --sku Standard_LRS \
    --kind StorageV2
  echo "  Storage account created: $STORAGE_ACCOUNT_NAME"
else
  echo "  Storage account already exists, skipping..."
fi

echo "Creating blob container (if not exists)..."
STORAGE_KEY=$(az storage account keys list --account-name $STORAGE_ACCOUNT_NAME --resource-group $RESOURCE_GROUP --query "[0].value" -o tsv)
if ! az storage container show --name $BLOB_CONTAINER_NAME --account-name $STORAGE_ACCOUNT_NAME --account-key "$STORAGE_KEY" &>/dev/null; then
  az storage container create \
    --name $BLOB_CONTAINER_NAME \
    --account-name $STORAGE_ACCOUNT_NAME \
    --account-key "$STORAGE_KEY"
  echo "  Blob container created: $BLOB_CONTAINER_NAME"
else
  echo "  Blob container already exists, skipping..."
fi

AZURE_STORAGE_CONNECTION_STRING=$(az storage account show-connection-string --name $STORAGE_ACCOUNT_NAME --resource-group $RESOURCE_GROUP --query connectionString -o tsv)
az keyvault secret set \
  --vault-name $KEYVAULT_NAME \
  --name "azure-storage-connection-string" \
  --value "$AZURE_STORAGE_CONNECTION_STRING" &>/dev/null
echo "  Stored azure-storage-connection-string in Key Vault"

echo "Creating Azure Cache for Redis (optional - can be skipped to reduce cost)..."
if ! az redis show --name $REDIS_NAME --resource-group $RESOURCE_GROUP &>/dev/null; then
  az redis create \
    --name $REDIS_NAME \
    --resource-group $RESOURCE_GROUP \
    --location $LOCATION \
    --sku Basic \
    --vm-size c0

  echo "  Waiting for Redis to be ready (this takes a few minutes)..."
  az redis wait --name $REDIS_NAME --resource-group $RESOURCE_GROUP --created

  REDIS_KEY=$(az redis list-keys --name $REDIS_NAME --resource-group $RESOURCE_GROUP --query primaryKey -o tsv)
  REDIS_HOST="${REDIS_NAME}.redis.cache.windows.net"
  REDIS_CONNECTION_STRING="${REDIS_HOST}:6380,password=${REDIS_KEY},ssl=True,abortConnect=False"

  az keyvault secret set \
    --vault-name $KEYVAULT_NAME \
    --name "redis-connection-string" \
    --value "$REDIS_CONNECTION_STRING" &>/dev/null
  echo "  Stored redis-connection-string in Key Vault"
else
  echo "  Redis already exists, skipping..."
fi

echo "Creating service principal for GitHub Actions..."
SP_APP_ID=""
SP_OUTPUT=$(az ad sp create-for-rbac \
  --name "$SP_NAME" \
  --role contributor \
  --scopes /subscriptions/$(az account show --query id -o tsv)/resourceGroups/$RESOURCE_GROUP \
  --sdk-auth 2>/dev/null || echo "Service principal may already exist - create manually if needed")
if [ "$SP_OUTPUT" != "Service principal may already exist - create manually if needed" ]; then
  SP_APP_ID=$(echo "$SP_OUTPUT" | grep -o '"clientId": "[^"]*"' | cut -d'"' -f4)
fi
if [ -z "$SP_APP_ID" ]; then
  SP_APP_ID=$(az ad sp list --display-name "$SP_NAME" --query "[0].appId" -o tsv 2>/dev/null || true)
fi

echo "Configuring Container Apps (RBAC - no connection string secrets)..."
STORAGE_CONN=$(az keyvault secret show --vault-name $KEYVAULT_NAME --name azure-storage-connection-string --query value -o tsv 2>/dev/null || true)

for APP in "$APP_NAME" "${APP_NAME}-staging"; do
  echo "  Configuring $APP..."

  az containerapp identity assign \
    --name $APP \
    --resource-group $RESOURCE_GROUP \
    --system-assigned 2>/dev/null || true

  IDENTITY_ID=$(az containerapp show --name $APP --resource-group $RESOURCE_GROUP --query "identity.principalId" -o tsv)
  if [ -n "$IDENTITY_ID" ]; then
    az role assignment create \
      --role "Key Vault Secrets User" \
      --assignee $IDENTITY_ID \
      --scope /subscriptions/$(az account show --query id -o tsv)/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.KeyVault/vaults/$KEYVAULT_NAME 2>/dev/null || true
  fi

  if [ -n "$STORAGE_CONN" ]; then
    az containerapp secret set --name $APP --resource-group $RESOURCE_GROUP \
      --secrets "azure-storage-connection-string=$STORAGE_CONN"
  fi

  ENV_NAME="Production"
  [ "$APP" = "${APP_NAME}-staging" ] && ENV_NAME="Staging"

  ENV_VARS=(
    "ASPNETCORE_ENVIRONMENT=$ENV_NAME"
    "DataStore__Type=Sql"
    "DataStore__SqlServer=$SQL_SERVER_NAME.database.windows.net"
    "DataStore__SqlDatabase=$SQL_DB_NAME"
  )
  [ -n "${CORS_ALLOWED_ORIGINS:-}" ] && ENV_VARS+=("Cors__AllowedOrigins=$CORS_ALLOWED_ORIGINS")

  az containerapp update --name $APP --resource-group $RESOURCE_GROUP \
    --set-env-vars "${ENV_VARS[@]}" 2>/dev/null || true
done

echo "Creating database users for RBAC (OBJECT_ID-based)..."
PRINCIPAL_PROD=$(az containerapp show --name $APP_NAME --resource-group $RESOURCE_GROUP --query "identity.principalId" -o tsv 2>/dev/null || true)
PRINCIPAL_STAGING=$(az containerapp show --name "${APP_NAME}-staging" --resource-group $RESOURCE_GROUP --query "identity.principalId" -o tsv 2>/dev/null || true)
PRINCIPAL_SP=$(az ad sp list --display-name "$SP_NAME" --query "[0].id" -o tsv 2>/dev/null || true)

RBAC_SQL="
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = '$APP_NAME')
  CREATE USER [$APP_NAME] FROM EXTERNAL PROVIDER WITH OBJECT_ID = '${PRINCIPAL_PROD}';
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = '${APP_NAME}-staging')
  CREATE USER [${APP_NAME}-staging] FROM EXTERNAL PROVIDER WITH OBJECT_ID = '${PRINCIPAL_STAGING}';
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = '$SP_NAME')
  CREATE USER [$SP_NAME] FROM EXTERNAL PROVIDER WITH OBJECT_ID = '${PRINCIPAL_SP}';
IF IS_ROLEMEMBER('db_datareader', '$APP_NAME') = 0 ALTER ROLE db_datareader ADD MEMBER [$APP_NAME];
IF IS_ROLEMEMBER('db_datawriter', '$APP_NAME') = 0 ALTER ROLE db_datawriter ADD MEMBER [$APP_NAME];
IF IS_ROLEMEMBER('db_ddladmin', '$APP_NAME') = 0 ALTER ROLE db_ddladmin ADD MEMBER [$APP_NAME];
IF IS_ROLEMEMBER('db_datareader', '${APP_NAME}-staging') = 0 ALTER ROLE db_datareader ADD MEMBER [${APP_NAME}-staging];
IF IS_ROLEMEMBER('db_datawriter', '${APP_NAME}-staging') = 0 ALTER ROLE db_datawriter ADD MEMBER [${APP_NAME}-staging];
IF IS_ROLEMEMBER('db_ddladmin', '${APP_NAME}-staging') = 0 ALTER ROLE db_ddladmin ADD MEMBER [${APP_NAME}-staging];
IF IS_ROLEMEMBER('db_datareader', '$SP_NAME') = 0 ALTER ROLE db_datareader ADD MEMBER [$SP_NAME];
IF IS_ROLEMEMBER('db_datawriter', '$SP_NAME') = 0 ALTER ROLE db_datawriter ADD MEMBER [$SP_NAME];
IF IS_ROLEMEMBER('db_ddladmin', '$SP_NAME') = 0 ALTER ROLE db_ddladmin ADD MEMBER [$SP_NAME];
"

if command -v sqlcmd &>/dev/null && [ -n "$CURRENT_USER_UPN" ] && [ -n "$PRINCIPAL_PROD" ]; then
  echo "$RBAC_SQL" | sqlcmd -S "$SQL_SERVER_NAME.database.windows.net" -d "$SQL_DB_NAME" -G -U "$CURRENT_USER_UPN" -l 30 -N 2>/dev/null && \
    echo "  Database users created successfully (sqlcmd)" || echo "  sqlcmd failed - run infra/create-db-users-rbac.sql manually"
else
  echo "  Run infra/create-db-users-rbac.sql manually in Azure Portal > Query Editor (as Entra admin)"
fi

echo ""
echo "=========================================="
echo "SETUP COMPLETE - RBAC (no connection string secrets)"
echo "=========================================="
echo ""
echo "Resources:"
echo "  - Resource Group: $RESOURCE_GROUP"
echo "  - Container Registry: ${ACR_NAME}.azurecr.io"
echo "  - Container Apps: $APP_NAME, ${APP_NAME}-staging"
echo "  - SQL Server: ${SQL_SERVER_NAME}.database.windows.net (DB: $SQL_DB_NAME)"
echo "  - Key Vault: $KEYVAULT_NAME"
echo "  - Storage: $STORAGE_ACCOUNT_NAME ($BLOB_CONTAINER_NAME)"
echo "  - Redis: ${REDIS_NAME}.redis.cache.windows.net"
echo ""
echo "Database: RBAC/managed identity - no connection string stored."
echo "Container Apps use DefaultAzureCredential to connect."
echo ""
echo "NEXT STEPS:"
echo ""
echo "0. If DB users were not created: Run infra/create-db-users-rbac.sql in Azure Portal > Query Editor (as SQL Entra Admin)"
echo ""
echo "1. Add GitHub repository secrets (Settings → Secrets and variables → Actions):"
echo "   - AZURE_CREDENTIALS: (JSON below) - for OIDC, use client-id, tenant-id, subscription-id instead"
echo "   - AZURE_SQL_CONNECTION_STRING: (passwordless, for EF migrations):"
echo "     Server=tcp:${SQL_SERVER_NAME}.database.windows.net,1433;Database=${SQL_DB_NAME};Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
echo "     (No password - migrations use DefaultAzureCredential after az login)"
echo ""
echo "2. Create GitHub Environments: staging, production (Settings → Environments)"
echo "   Optionally add required reviewers for production."
echo ""
echo "3. (Optional) Configure OIDC for passwordless Azure login - see infra/README.md"
echo ""
if [ "$SP_OUTPUT" != "Service principal may already exist - create manually if needed" ]; then
  echo "AZURE_CREDENTIALS (add as GitHub secret for SP-based auth):"
  echo "$SP_OUTPUT"
else
  echo "To create/update the service principal manually:"
  echo "  az ad sp create-for-rbac --name \"$SP_NAME\" --role contributor --scopes /subscriptions/\$(az account show -q id)/resourceGroups/$RESOURCE_GROUP --sdk-auth"
fi
