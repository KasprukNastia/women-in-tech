# =======================================================================================================================
# setup-remote-app-to-key-vault-access.ps1
# =======================================================================================================================
# This script sets up access to the key vault in the current tenant for the remote enterprise application
#
# Usage:
#   .\setup-remote-app-to-key-vault-access.ps1 -RESOURCE_PREFIX <prefix> -REMOTE_APP_IDS <remote-app-id>, <remote-app-id>
# =======================================================================================================================

[CmdletBinding()]
param (
    [Parameter(Mandatory=$True)]
    [string]$RESOURCE_PREFIX, 

    [Parameter(Mandatory=$True)]
    [string[]]$REMOTE_APP_IDS
)

$RESOURCE_GROUP_NAME = $RESOURCE_PREFIX + "2RG"

$KEYVAULT_NAME = $RESOURCE_PREFIX + "2KV"


$KEYVAULT_RESOURCE_ID=$(az keyvault show --resource-group $RESOURCE_GROUP_NAME --name $KEYVAULT_NAME --query id --output tsv 2>$null)
if (-not $KEYVAULT_RESOURCE_ID) {
	Write-Host "Key Vault '$KEYVAULT_NAME' is not found in the resource group '$RESOURCE_GROUP_NAME'." -ForegroundColor Red
	exit
}

foreach ($APP_CLIENT_ID in $REMOTE_APP_IDS) {
	Write-Host "Provisionning remote application '$APP_CLIENT_ID'" -ForegroundColor Yellow	

	$SP_ID = az ad sp show --id $APP_CLIENT_ID --output tsv 2>$null
	if (-not $SP_ID) {
		az ad sp create --id $APP_CLIENT_ID
		Write-Host "Remote application '$APP_CLIENT_ID' has been provisionned successfully."
		continue
	}
	else {
		Write-Host "Remote application '$APP_CLIENT_ID' has been already provisionned."
	}

	Write-Host "Assigning Key Vault '$KEYVAULT_NAME' Key Vault Secrets Officer role for the remote application '$APP_CLIENT_ID'" -ForegroundColor Yellow
	az role assignment create --assignee $APP_CLIENT_ID --role "Key Vault Secrets Officer" --scope "${KEYVAULT_RESOURCE_ID}"
}


# to delete service principle, run the following command
#az ad sp delete --id $APP_CLIENT_ID