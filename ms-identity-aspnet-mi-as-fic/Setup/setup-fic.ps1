# =============================================================================
# setup-fic.ps1
# =============================================================================
# This script sets up the federated identity credential for managed identity
#
# Usage:
#   .\setup-fic.ps1 -RESOURCE_PREFIX <prefix> -TENANT <tenant-id> -CONFEDENTIAL_APP_ID <app-id>
# =============================================================================

[CmdletBinding()]
param (
    [Parameter(Mandatory=$True, HelpMessage='Prefix used for resource naming')]
    [string]$RESOURCE_PREFIX,

    [Parameter(Mandatory=$True, HelpMessage='Entra Tenant ID')]
    [string]$TENANT,

    [Parameter(Mandatory=$True, HelpMessage='Confidential App ID')]
    [string]$CONFEDENTIAL_APP_ID
)

Write-Host "Creating the Federated Identity Credential..." -ForegroundColor Yellow

$RESOURCE_GROUP_NAME = $RESOURCE_PREFIX + "2RG"

# Get managed identity principal ID
$MANAGED_IDENTITY_NAME = $RESOURCE_PREFIX + "2MI"
$MANAGED_IDENTITY_PRINCIPAL_ID = $(az identity show --name $MANAGED_IDENTITY_NAME --resource-group $RESOURCE_GROUP_NAME --query "principalId" -o tsv)

# Define the JSON content for FIC
$jsonContent = @{
    name        = $RESOURCE_PREFIX + "2MiFic"
    issuer      = "https://login.microsoftonline.com/$TENANT/v2.0"
    subject     = "$MANAGED_IDENTITY_PRINCIPAL_ID"
    description = "Sample using Managed Identity as a federated identity credential (FIC)"
    audiences   = @("api://AzureADTokenExchange")
}

$jsonString = $jsonContent | ConvertTo-Json -Depth 2
$outputFilePath = "fic-credential-config.json"
Set-Content -Path $outputFilePath -Value $jsonString
Write-Host "JSON file generated successfully at: $outputFilePath" -ForegroundColor Green

# Create the federated identity credential
az ad app federated-credential create --id $CONFEDENTIAL_APP_ID --parameters fic-credential-config.json

# Clean up the temporary file
Remove-Item -Path $outputFilePath -Force

Write-Host "Federated Identity Credential created successfully!" -ForegroundColor Green 
