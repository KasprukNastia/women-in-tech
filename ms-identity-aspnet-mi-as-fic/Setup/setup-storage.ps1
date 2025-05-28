# =====================================================================================================
# setup-storage.ps1
# =====================================================================================================
# This script sets up Azure Blob Storage
#
# Usage:
#   .\setup-storage.ps1 -RESOURCE_PREFIX <prefix> -SUBSCRIPTION <subscription-id> -LOCATION <location>
# =====================================================================================================

[CmdletBinding()]
param (
    [Parameter(Mandatory=$True)]
    [string]$RESOURCE_PREFIX, 

    [Parameter(Mandatory=$True)]
    [string]$LOCATION,

    [Parameter(Mandatory=$True)]
    [string]$SUBSCRIPTION
)

$RESOURCE_GROUP_NAME = $RESOURCE_PREFIX + "2RG"

$STORAGE_ACCOUNT_NAME = ($RESOURCE_PREFIX + "2SA").ToLower()

Write-Host "Creating storage account..." -ForegroundColor Yellow
# Check if the storage account exists
$ACCOUNT = az storage account show `
    --name $STORAGE_ACCOUNT_NAME `
    --resource-group $RESOURCE_GROUP_NAME `
    --query "name" `
    --output tsv 2>$null
# Create the storage account only if it doesn't exist
if (-not $ACCOUNT) {
    az storage account create --name $STORAGE_ACCOUNT_NAME --resource-group $RESOURCE_GROUP_NAME --location $LOCATION --sku Standard_LRS --kind StorageV2
    Write-Host "Storage account '$STORAGE_ACCOUNT_NAME' created."
} else {
    Write-Host "Storage account '$STORAGE_ACCOUNT_NAME' already exists."
}

Write-Host "Creating storage container..." -ForegroundColor Yellow
$CONTAINER_NAME = ($RESOURCE_PREFIX + "2Container").ToLower()
az storage container create --name $CONTAINER_NAME --account-name $STORAGE_ACCOUNT_NAME

@{
    StorageAccountName = $STORAGE_ACCOUNT_NAME
    ContainerName = $CONTAINER_NAME
} 