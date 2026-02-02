#!/usr/bin/env bash
# Set GitHub secrets for TaskAgent from a local .env file.
# Create .github-secrets.env in the repo root (add to .gitignore) with:
#
#   AZURE_CREDENTIALS='{"clientId":"...","clientSecret":"...","subscriptionId":"...","tenantId":"..."}'
#   STAGING_SQL_CONNECTION_STRING='Server=tcp:taskagent-sql-eus2.database.windows.net,1433;Database=taskagent;Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
#   PRODUCTION_SQL_CONNECTION_STRING='Server=tcp:taskagent-sql-eus2.database.windows.net,1433;Database=taskagent;Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
#
# Then run: ./scripts/set-github-secrets.sh

set -e
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
SECRETS_FILE="$REPO_ROOT/.github-secrets.env"

if [ ! -f "$SECRETS_FILE" ]; then
  echo "Create $SECRETS_FILE with your secrets first. See script header for format."
  exit 1
fi

cd "$REPO_ROOT"
source "$SECRETS_FILE"

for name in AZURE_CREDENTIALS STAGING_SQL_CONNECTION_STRING PRODUCTION_SQL_CONNECTION_STRING; do
  val="${!name}"
  if [ -z "$val" ]; then
    echo "Skipping $name (empty)"
  else
    echo "$val" | gh secret set "$name"
    echo "Set $name"
  fi
done

echo "Done."
