# ========================================================================================
# setup-webapp.ps1
# ========================================================================================
# This script sets up Azure Web App
#
# Usage:
#   .\setup-webapp.ps1 -RESOURCE_PREFIX <prefix> -USER_ASSIGNED_RESOURCE_ID <resource-id>
# ========================================================================================

[CmdletBinding()]
param (
    [Parameter(Mandatory=$True)]
    [string]$RESOURCE_PREFIX,

    [Parameter(Mandatory=$True)]
    [string]$USER_ASSIGNED_RESOURCE_ID
)

$RESOURCE_GROUP_NAME = $RESOURCE_PREFIX + "2RG"

Write-Host "Creating web app and app service plan..." -ForegroundColor Yellow
$APP_PLAN_NAME = $RESOURCE_PREFIX + "2AppPlan"
$WEB_APP_NAME = $RESOURCE_PREFIX + "2WebApp"

Start-Sleep -Seconds 2

Write-Host "Creating app service plan..." -ForegroundColor Yellow
az appservice plan create --name $APP_PLAN_NAME --resource-group $RESOURCE_GROUP_NAME --sku FREE --only-show-errors

Write-Host "Creating web app..." -ForegroundColor Yellow
az webapp create --name $WEB_APP_NAME --resource-group $RESOURCE_GROUP_NAME --plan $APP_PLAN_NAME

$WEB_APP_URL = "https://" + $(az webapp show --name $WEB_APP_NAME --resource-group $RESOURCE_GROUP_NAME --query "hostNames[0]" -o tsv)
az webapp identity assign --resource-group $RESOURCE_GROUP_NAME --name $WEB_APP_NAME --identities $USER_ASSIGNED_RESOURCE_ID

Write-Host "Configuring web app logging..." -ForegroundColor Yellow
az webapp log config --name $WEB_APP_NAME --resource-group $RESOURCE_GROUP_NAME --application-logging filesystem --level Verbose

Write-Host "app logging configured" -ForegroundColor Green
Write-Host "to view logs, run: az webapp log tail --name $WEB_APP_NAME --resource-group $RESOURCE_GROUP_NAME" -ForegroundColor Yellow

az webapp config appsettings set -n $WEB_APP_NAME -g $RESOURCE_GROUP_NAME --settings ASPNETCORE_ENVIRONMENT=Development --only-show-errors
az webapp config appsettings set -n $WEB_APP_NAME -g $RESOURCE_GROUP_NAME --settings AuthConfig__UseManagedIdentity=false --only-show-errors
az webapp config appsettings set -n $WEB_APP_NAME -g $RESOURCE_GROUP_NAME --settings KeyVaultConfig__Remote__Uri= --only-show-errors
az webapp config appsettings set -n $WEB_APP_NAME -g $RESOURCE_GROUP_NAME --settings KeyVaultConfig__Remote__SecretName= --only-show-errors

# Return the web app details
@{
    WebAppUrl = $WEB_APP_URL
    WebAppName = $WEB_APP_NAME
} 
