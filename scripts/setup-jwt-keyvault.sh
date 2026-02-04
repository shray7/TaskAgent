#!/bin/bash
# Generate a JWT signing key, store it in Azure Key Vault, and configure both
# Container Apps (staging + production) to use it via Key Vault reference.
# Run after infra/setup-azure.sh (or ensure Key Vault and Container Apps exist).
# Requires: az CLI, openssl, and Azure login (az login).

set -e

# Align with deploy.yml and infra/setup-azure.sh
RESOURCE_GROUP="${RESOURCE_GROUP:-rg-taskagent}"
KEYVAULT_NAME="${KEYVAULT_NAME:-taskagentflow-kv}"
APP_NAME="${APP_NAME:-taskagent-api}"
SQL_SERVER_NAME="${SQL_SERVER_NAME:-taskagent-sql-eus2}"
SQL_DB_NAME="${SQL_DB_NAME:-taskagent}"
# Optional: set CORS e.g. for GitHub Pages
# CORS_ALLOWED_ORIGINS="https://youruser.github.io"

JWT_SECRET_NAME="TaskAgent-JwtKey"
CONTAINER_APP_SECRET_NAME="jwt-key"

echo "Using Key Vault: $KEYVAULT_NAME, Resource Group: $RESOURCE_GROUP"
echo ""

# 1. Generate a secure key (48 bytes = 64 base64 chars, suitable for HMAC-SHA256)
echo "Generating JWT signing key..."
JWT_KEY=$(openssl rand -base64 48 | tr -d '\n')
if [ -z "$JWT_KEY" ]; then
  echo "ERROR: Failed to generate key (openssl rand)"
  exit 1
fi

# 2. Store in Key Vault (create or update)
echo "Storing secret in Key Vault as $JWT_SECRET_NAME..."
az keyvault secret set \
  --vault-name "$KEYVAULT_NAME" \
  --name "$JWT_SECRET_NAME" \
  --value "$JWT_KEY" \
  --output none
echo "  Done. Secret is in Key Vault (value not shown)."
echo ""

# 3. Configure each Container App: add secret from Key Vault ref, then set env var
KEYVAULT_URI="https://${KEYVAULT_NAME}.vault.azure.net/secrets/${JWT_SECRET_NAME}"
# identityref:system = use system-assigned managed identity
SECRET_REF="jwt-key=keyvaultref:${KEYVAULT_URI},identityref:system"

for APP in "$APP_NAME" "${APP_NAME}-staging"; do
  echo "Configuring Container App: $APP"

  # Ensure managed identity exists and can read Key Vault
  az containerapp identity assign \
    --name "$APP" \
    --resource-group "$RESOURCE_GROUP" \
    --system-assigned 2>/dev/null || true

  IDENTITY_ID=$(az containerapp show --name "$APP" --resource-group "$RESOURCE_GROUP" --query "identity.principalId" -o tsv 2>/dev/null || true)
  if [ -n "$IDENTITY_ID" ]; then
    KV_SCOPE="/subscriptions/$(az account show --query id -o tsv)/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.KeyVault/vaults/$KEYVAULT_NAME"
    az role assignment create \
      --role "Key Vault Secrets User" \
      --assignee "$IDENTITY_ID" \
      --scope "$KV_SCOPE" 2>/dev/null || true
  fi

  # Add secret to container app (Key Vault reference)
  az containerapp secret set \
    --name "$APP" \
    --resource-group "$RESOURCE_GROUP" \
    --secrets "$SECRET_REF"
  echo "  Added secret $CONTAINER_APP_SECRET_NAME (Key Vault ref)."

  # Set env var to use the secret (must include existing vars or we overwrite)
  ENV_NAME="Production"
  [ "$APP" = "${APP_NAME}-staging" ] && ENV_NAME="Staging"
  ENV_VARS=(
    "ASPNETCORE_ENVIRONMENT=$ENV_NAME"
    "DataStore__Type=Sql"
    "DataStore__SqlServer=$SQL_SERVER_NAME.database.windows.net"
    "DataStore__SqlDatabase=$SQL_DB_NAME"
    "Jwt__Key=secretref:$CONTAINER_APP_SECRET_NAME"
  )
  [ -n "${CORS_ALLOWED_ORIGINS:-}" ] && ENV_VARS+=("Cors__AllowedOrigins=$CORS_ALLOWED_ORIGINS")

  az containerapp update --name "$APP" --resource-group "$RESOURCE_GROUP" \
    --set-env-vars "${ENV_VARS[@]}"
  echo "  Set env vars (including Jwt__Key from secret)."
  echo ""
done

echo "=========================================="
echo "JWT Key Vault setup complete"
echo "=========================================="
echo ""
echo "  Key Vault secret: $KEYVAULT_NAME / $JWT_SECRET_NAME"
echo "  Container Apps: $APP_NAME, ${APP_NAME}-staging now use Jwt__Key from Key Vault."
echo ""
echo "Note: If you had other env vars (e.g. ConnectionStrings) set in the portal,"
echo "re-add them there; this script set only the list above."
echo ""
