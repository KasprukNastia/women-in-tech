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
az storage account create --name $STORAGE_ACCOUNT_NAME --resource-group $RESOURCE_GROUP_NAME --location $LOCATION --sku Standard_LRS --kind StorageV2

Write-Host "Creating storage container..." -ForegroundColor Yellow
$CONTAINER_NAME = ($RESOURCE_PREFIX + "2Container").ToLower()
az storage container create --name $CONTAINER_NAME --account-name $STORAGE_ACCOUNT_NAME

@{
    StorageAccountName = $STORAGE_ACCOUNT_NAME
    ContainerName = $CONTAINER_NAME
} 