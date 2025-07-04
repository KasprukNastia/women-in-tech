# ==================================================================================================================================================
# setup-key-vault.ps1
# ==================================================================================================================================================
# This script sets up the key vault in current tenant.
#
# Usage:
#   .\setup-key-vault.ps1 -RESOURCE_PREFIX <prefix> -SUBSCRIPTION <subscription-id> -LOCATION <location> -USER_EMAIL <email> -APP_CLIENT_ID <app-id>
# ==================================================================================================================================================

[CmdletBinding()]
param (
    [Parameter(Mandatory=$True)]
    [string]$RESOURCE_PREFIX,

    [Parameter(Mandatory=$True)]
    [string]$SUBSCRIPTION,

    [Parameter(Mandatory=$True)]
    [string]$LOCATION,

    [Parameter(Mandatory=$True)]
    [string]$USER_EMAIL,

    [Parameter(Mandatory=$True)]
    [string]$APP_CLIENT_ID
)

$RESOURCE_GROUP_NAME = $RESOURCE_PREFIX + "2RG"

#### 4. Create a key vault
$KEYVAULT_NAME = $RESOURCE_PREFIX + "2KV"
Write-Host "Creating Key Vault '$KEYVAULT_NAME'... " -ForegroundColor Yellow
az keyvault create --name $KEYVAULT_NAME --resource-group $RESOURCE_GROUP_NAME --location $LOCATION --enable-rbac-authorization

Write-Host "Assigning Key Vault admin role..." -ForegroundColor Yellow
Start-Sleep -Seconds 3

$KEYVAULT_RESOURCE_ID=$(az keyvault show --resource-group $RESOURCE_GROUP_NAME --name $KEYVAULT_NAME --query id --output tsv)

if ($USER_EMAIL) {
    az role assignment create --assignee "${USER_EMAIL}" --role "Key Vault Secrets Officer" --scope "${KEYVAULT_RESOURCE_ID}"
}

if ($APP_CLIENT_ID) {
    # az ad sp create --id $APP_CLIENT_ID 
    az role assignment create --assignee $APP_CLIENT_ID --role "Key Vault Secrets Officer" --scope "${KEYVAULT_RESOURCE_ID}"
}

$SECRET_NAME = $RESOURCE_PREFIX + "2SECRET"
Write-Host "Creating a secret '$SECRET_NAME' in Key Vault..." -ForegroundColor Yellow
az keyvault secret set --vault-name $KEYVAULT_NAME --name $SECRET_NAME --value "This is a secret '$SECRET_NAME' from KV '$KEYVAULT_NAME' in subscription '$SUBSCRIPTION'!"

return @{
    KeyVaultName = $KEYVAULT_NAME
    KeyVaultResourceId = $KEYVAULT_RESOURCE_ID
    SecretName = $SECRET_NAME
}