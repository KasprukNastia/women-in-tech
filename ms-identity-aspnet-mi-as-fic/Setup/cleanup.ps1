# =======================================================================================
# cleanup.ps1
# =======================================================================================
# This script cleans up resources created during the setup execution
#
# Usage:
#   .\cleanup.ps1 -TENANT <tenant> -SUBSCRIPTION <subscription> -RESOURCE_PREFIX <prefix>
# =======================================================================================

param (

    [Parameter(Mandatory=$True, HelpMessage='Tenant ID')]
    [string]$TENANT,

    [Parameter(Mandatory=$True, HelpMessage='Azure Subscription ID')]
    [string]$SUBSCRIPTION,
    
    [Parameter(Mandatory=$True, HelpMessage='Resource Prefix used for naming the resources')]
    [string]$RESOURCE_PREFIX,

    [Parameter(Mandatory=$False, HelpMessage='A different tenant to create the Key Vault in. Will create it in the same tenant as the other resources if not provided')]
    [string]$KV_TENANT,

    [Parameter(Mandatory=$False, HelpMessage='The subscription to create the keyvault in. Will create it in the same tenant as the other resources if not provided')]
    [string]$KV_SUBSCRIPTION
)

try {
    az account set --subscription $SUBSCRIPTION
    Write-Host "User is already signed in..."
}
catch {    
    Write-Host "Logging into Azure..." -ForegroundColor Yellow
    Write-Host "Tenant id $TENANT" -ForegroundColor Green
    az login --tenant $TENANT 
    Write-Host "setting subscription $SUBSCRIPTION" -ForegroundColor Green
    az account set --subscription $SUBSCRIPTION
}

$USER_EMAIL = az ad signed-in-user show --query "userPrincipalName" -o tsv
Write-Host "Welcome: $USER_EMAIL!" -ForegroundColor Cyan
Start-Sleep -Milliseconds 100

$RESOURCE_GROUP_NAME = $RESOURCE_PREFIX + "2RG"

Write-Host "Starting cleanup process..." -ForegroundColor Yellow

Write-Host "Deleting resource group $RESOURCE_GROUP_NAME..." -ForegroundColor Yellow
az group delete --name $RESOURCE_GROUP_NAME --no-wait --yes

# Delete App Registration
Write-Host "Deleting app registration..." -ForegroundColor Yellow
$APP_REG_NAME = $RESOURCE_PREFIX + "2AppReg"
$APP_REG_ID = az ad app list --display-name $APP_REG_NAME --query "[].appId" --output tsv
if ($APP_REG_ID) {
    az ad app delete --id $APP_REG_ID
    Write-Host "Deleted App Registration: $APP_REG_NAME" -ForegroundColor Green
} else {
    Write-Host "No App Registration found with name: $APP_REG_NAME" -ForegroundColor Cyan
}

if ($KV_TENANT -and $KV_SUBSCRIPTION) {
    Write-Host "we need to cleanup the keyvault in a different tenant, too. Please login to the keyvault tenant" 

    Start-Sleep -Milliseconds 500
    try {
        az account set --subscription $KV_SUBSCRIPTION
        Write-Host "User is already signed into the keyvault tenant..."
    }
    catch {  
        ## loging into the other tenant
        Write-Host "Logging into Azure..." -ForegroundColor Yellow
        Write-Host "Key vault Tenant id $KV_TENANT" -ForegroundColor Green
        az login --tenant $KV_TENANT 
        Write-Host "setting subscription $KV_SUBSCRIPTION" -ForegroundColor Green
        az account set --subscription $KV_SUBSCRIPTION
    }

    Write-Host "Deleting the keyvault tenant's resource group $RESOURCE_GROUP_NAME..." -ForegroundColor Yellow
    az group delete --name $RESOURCE_GROUP_NAME --no-wait --yes
}

Write-Host "Cleanup process completed!" -ForegroundColor Green