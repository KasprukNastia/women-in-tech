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

Write-Host "Creating managed identity..." -ForegroundColor Yellow
$MANAGED_IDENTITY_NAME = $RESOURCE_PREFIX + "2MI"
az identity create --name $MANAGED_IDENTITY_NAME --resource-group $RESOURCE_GROUP_NAME --location $LOCATION --subscription "${SUBSCRIPTION}"

$USER_ASSIGNED_CLIENT_ID = $(az identity show --resource-group $RESOURCE_GROUP_NAME --name $MANAGED_IDENTITY_NAME --query 'clientId' --output tsv)
$USER_ASSIGNED_RESOURCE_ID = $(az identity show --name $MANAGED_IDENTITY_NAME --resource-group $RESOURCE_GROUP_NAME --query id --output tsv)

# Return the managed identity details
@{
    UserAssignedClientId = $USER_ASSIGNED_CLIENT_ID
    UserAssignedResourceId = $USER_ASSIGNED_RESOURCE_ID
    ManagedIdentityName = $MANAGED_IDENTITY_NAME
} 