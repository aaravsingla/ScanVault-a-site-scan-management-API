#!/usr/bin/env bash
# Creates everything ScanVault needs on Azure, then connects GitHub Actions to it.
#
#   Azure SQL database (free offer)   -> sites, scans, annotations
#   Storage account + "scans" container -> the scan files (Blob Storage)
#   App Service (Linux, F1 free plan)  -> runs the API and serves the React app
#
# Prerequisites: `az login` and `gh auth login` done, run from the repository root.
# Usage:  bash infra/azure-setup.sh
set -euo pipefail

LOCATION="${LOCATION:-centralindia}"
RESOURCE_GROUP="${RESOURCE_GROUP:-scanvault-rg}"
SUFFIX="${SUFFIX:-$(LC_ALL=C tr -dc 'a-z0-9' </dev/urandom | head -c 6)}"

APP_NAME="scanvault-${SUFFIX}"
PLAN_NAME="scanvault-plan"
STORAGE_ACCOUNT="scanvault${SUFFIX}"
SQL_SERVER="scanvault-sql-${SUFFIX}"
SQL_DB="scanvault"
SQL_ADMIN="scanvaultadmin"
SQL_PASSWORD="${SQL_PASSWORD:-Sv$(LC_ALL=C tr -dc 'A-Za-z0-9' </dev/urandom | head -c 20)!9}"

echo "==> Resource group ${RESOURCE_GROUP} in ${LOCATION}"
az group create --name "$RESOURCE_GROUP" --location "$LOCATION" --output none

echo "==> Blob Storage account ${STORAGE_ACCOUNT}"
az storage account create --name "$STORAGE_ACCOUNT" --resource-group "$RESOURCE_GROUP" \
  --location "$LOCATION" --sku Standard_LRS --kind StorageV2 \
  --allow-blob-public-access false --min-tls-version TLS1_2 --output none
STORAGE_CONNECTION=$(az storage account show-connection-string \
  --name "$STORAGE_ACCOUNT" --resource-group "$RESOURCE_GROUP" --query connectionString -o tsv)
az storage container create --name scans --connection-string "$STORAGE_CONNECTION" --output none

echo "==> Azure SQL server ${SQL_SERVER} and free-offer database"
az sql server create --name "$SQL_SERVER" --resource-group "$RESOURCE_GROUP" --location "$LOCATION" \
  --admin-user "$SQL_ADMIN" --admin-password "$SQL_PASSWORD" --output none
# 0.0.0.0 is Azure's special rule meaning "allow other Azure services" (the App Service).
az sql server firewall-rule create --resource-group "$RESOURCE_GROUP" --server "$SQL_SERVER" \
  --name AllowAzureServices --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0 --output none
az sql db create --resource-group "$RESOURCE_GROUP" --server "$SQL_SERVER" --name "$SQL_DB" \
  --edition GeneralPurpose --family Gen5 --capacity 2 --compute-model Serverless \
  --use-free-limit --free-limit-exhaustion-behavior AutoPause --output none
SQL_CONNECTION="Server=tcp:${SQL_SERVER}.database.windows.net,1433;Database=${SQL_DB};User ID=${SQL_ADMIN};Password=${SQL_PASSWORD};Encrypt=True;TrustServerCertificate=False;Connection Timeout=60;"

echo "==> App Service ${APP_NAME} (Linux, F1 free)"
az appservice plan create --name "$PLAN_NAME" --resource-group "$RESOURCE_GROUP" \
  --location "$LOCATION" --is-linux --sku F1 --output none
az webapp create --name "$APP_NAME" --resource-group "$RESOURCE_GROUP" --plan "$PLAN_NAME" \
  --runtime "DOTNETCORE:10.0" --basic-auth Enabled --output none

echo "==> App configuration (switches the app from SQLite/local files to Azure SQL/Blob Storage)"
az webapp config appsettings set --name "$APP_NAME" --resource-group "$RESOURCE_GROUP" --output none --settings \
  Database__Provider=SqlServer \
  Storage__Provider=AzureBlob \
  Storage__AzureBlob__Container=scans \
  "Storage__AzureBlob__ConnectionString=${STORAGE_CONNECTION}"
az webapp config connection-string set --name "$APP_NAME" --resource-group "$RESOURCE_GROUP" \
  --connection-string-type SQLAzure --settings "ScanVault=${SQL_CONNECTION}" --output none

echo "==> Connecting GitHub Actions (secret + variable on this repository)"
az webapp deployment list-publishing-profiles --name "$APP_NAME" --resource-group "$RESOURCE_GROUP" --xml \
  | gh secret set AZURE_WEBAPP_PUBLISH_PROFILE
gh variable set AZURE_WEBAPP_NAME --body "$APP_NAME"

echo
echo "Done. Push to main (or re-run the CI/CD workflow) to deploy."
echo "App URL:        https://${APP_NAME}.azurewebsites.net"
echo "SQL admin user: ${SQL_ADMIN}  (password is stored only in the App Service connection string)"
