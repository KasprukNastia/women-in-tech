<#
.SYNOPSIS
This script automates the creation and configuration of Azure resources for a web application.

.DESCRIPTION
The script performs the following operations:
1. Logs into the Azure account and sets the specified subscription and tenant.
2. Creates a resource group in the specified location.
3. Provisions a storage account and container.
4. Creates a managed identity for secure access to resources.
5. Deploys an App Service Plan and Web App and assigns the managed identity to the Web App.
6. Registers an application in Azure AD, configures permissions, and creates a service principal.
7. Creates an Azure Key Vault in the same or a different tenant and sets up access roles and a secret.
8. Configures a Federated Identity Credential for the application.
9. Generates `appsettings.json` for use in an ASP.NET Core application.

.NOTE
This script assumes you have sufficient permissions to create resources in the specified Azure subscription and tenant. 
If the script encounters any errors, it will terminate and provide diagnostic information.

.DISCLAIMER
Running this script will incur costs in your Azure subscription based on the resources provisioned (e.g., storage accounts, web apps, Key Vaults).
Ensure you review and understand the script's operations before proceeding.

.PARAMETERS
- TENANT: The Azure AD Tenant ID where the resources will be created.
- SUBSCRIPTION: The Azure Subscription ID under which resources will be provisioned.
- RESOURCE_PREFIX: A prefix for naming Azure resources.
- LOCATION: The Azure region where resources will be created.

.EXAMPLE
.\setup.ps1 -RESOURCE_PREFIX "TPO1" -LOCATION northeurope

#>

[CmdletBinding()]
param (
  [Parameter(Mandatory=$True, HelpMessage='A prefix that will be used to name the resources')]
  [string]$RESOURCE_PREFIX,

  [Parameter(Mandatory=$True, HelpMessage='The Azure location where the resources will be created')]
  [string]$LOCATION
)

# Prompt user for confirmation
Write-Host @"
=============================================================================
          Code Critic Application Setup
=============================================================================
This script will create and configure multiple Azure resources, including:

1. Azure AD Applications:
   - Confidential Client (API) with Managed Identity Federation

2. Azure Resources:
   - Resource Group
   - Azure Storage Account and Blob Container
   - Azure Key Vault and a secret (will be created in a different tenant if specified)
   - App Service Plan and Web App
   - User-assigned Managed Identity

3. Security Configuration:
   - User and App permissions to access Storage, KeyVault, and Graph
   - Federated Credentials for the Managed Identity

Note:   This script may incur costs in your Azure subscription. 
    Be sure to run the cleanup.ps1 after testing.
=============================================================================
"@ -ForegroundColor Yellow

$proceed = Read-Host "Do you agree to proceed? (Yes/No)"
if ($proceed -notmatch "^(y|yes)$") {
  Write-Host "Script execution aborted." -ForegroundColor Red
  exit
}

if (-not $RESOURCE_PREFIX) {
  $RESOURCE_PREFIX = Read-Host -Prompt "Please enter a prefix to use while naming created resources"
}
if (-not $LOCATION) {
  $LOCATION = Read-Host -Prompt "Please enter the location where the resources will be created"
}

Write-Host "Resource prefix: $RESOURCE_PREFIX"
Write-Host "Location: $LOCATION"

# Proceed with the script execution
Write-Host "Starting the setup process..." -ForegroundColor Green

Write-Host "############## Step 1: Get User Information ##############" -ForegroundColor Yellow
$userConfig = .\setup-user.ps1
if ($LASTEXITCODE -ne 0) {
  Write-Host "User login failed. Exiting script."
  exit 1
}

$SUBSCRIPTION = $userConfig.Subscription
$TENANT = $userConfig.Tenant
$CURRENT_USER_EMAIL = $userConfig.UserEmail
$DOMAIN_NAME = $userConfig.DomainName

Write-Host "############## Step 2: Create a Resource Group ##############" -ForegroundColor Yellow
start-sleep -Milliseconds 500
$RESOURCE_GROUP_NAME = $RESOURCE_PREFIX + "2RG"
az group create --name $RESOURCE_GROUP_NAME --location $LOCATION

Write-Host "############## Step 3: Create Storage Account and Container ##############" -ForegroundColor Yellow
start-sleep -Milliseconds 500
$storageConfig = .\setup-storage.ps1 -RESOURCE_PREFIX $RESOURCE_PREFIX -LOCATION $LOCATION -SUBSCRIPTION $SUBSCRIPTION

$STORAGE_ACCOUNT_NAME = $storageConfig.StorageAccountName
$CONTAINER_NAME = $storageConfig.ContainerName

Write-Host "############## Step 4: Create Managed Identity ##############" -ForegroundColor Yellow
$managedIdentityConfig = .\setup-managed-identity.ps1 -RESOURCE_PREFIX $RESOURCE_PREFIX -LOCATION $LOCATION -SUBSCRIPTION $SUBSCRIPTION
$USER_ASSIGNED_CLIENT_ID = $managedIdentityConfig.UserAssignedClientId
$USER_ASSIGNED_RESOURCE_ID = $managedIdentityConfig.UserAssignedResourceId

Write-Host "############## Step 5: Create Web App and Assign Managed Identity ##############" -ForegroundColor Yellow
start-sleep -Milliseconds 500
$webAppConfig = .\setup-webapp.ps1 -RESOURCE_PREFIX $RESOURCE_PREFIX -USER_ASSIGNED_RESOURCE_ID $USER_ASSIGNED_RESOURCE_ID
$WEB_APP_URL = $webAppConfig.WebAppUrl
$WEB_APP_NAME = $webAppConfig.WebAppName
az webapp config appsettings set -n $WEB_APP_NAME -g $RESOURCE_GROUP_NAME --settings AuthConfig__TenantId=$TENANT --only-show-errors
az webapp config appsettings set -n $WEB_APP_NAME -g $RESOURCE_GROUP_NAME --settings AuthConfig__ManagedIdentityClientId=$USER_ASSIGNED_CLIENT_ID --only-show-errors

Write-Host "############## Step 6: Create App Registration ##############" -ForegroundColor Yellow
start-sleep -Milliseconds 500
$confedentialAppConfig = .\setup-confidential-app.ps1 -RESOURCE_PREFIX $RESOURCE_PREFIX -WEB_APP_URL $WEB_APP_URL
$APP_CLIENT_ID = $confedentialAppConfig.ConfidentialAppId
$APP_REG_NAME = $confedentialAppConfig.AppRegistrationName
az webapp config appsettings set -n $WEB_APP_NAME -g $RESOURCE_GROUP_NAME --settings AuthConfig__AppClientId=$APP_CLIENT_ID --only-show-errors

Write-Host "Assigning Storage Blob Data Contributor role to the app '$APP_CLIENT_ID' in storage account '$STORAGE_ACCOUNT_NAME'" -ForegroundColor Yellow
az role assignment create --assignee $APP_CLIENT_ID --role "Storage Blob Data Contributor" --scope "/subscriptions/$SUBSCRIPTION/resourceGroups/$RESOURCE_GROUP_NAME/providers/Microsoft.Storage/storageAccounts/$STORAGE_ACCOUNT_NAME"

Write-Host "############## Step 7: Key Vault Creation (Same Tenant) ##############" -ForegroundColor Yellow
start-sleep -Milliseconds 500

$keyVaultConfig = .\setup-key-vault.ps1 -RESOURCE_PREFIX $RESOURCE_PREFIX -SUBSCRIPTION $SUBSCRIPTION -LOCATION $LOCATION -USER_EMAIL $CURRENT_USER_EMAIL -APP_CLIENT_ID $APP_CLIENT_ID
$KEYVAULT_NAME = $keyVaultConfig.KeyVaultName
$SECRET_NAME = $keyVaultConfig.SecretName

Write-Host "############## Step 8: Create Federated Identity Credential ##############" -ForegroundColor Yellow
# Make sure the user is logged in to the correct tenant
az account set --subscription $SUBSCRIPTION
$ficConfig = .\setup-fic.ps1 -RESOURCE_PREFIX $RESOURCE_PREFIX -CONFEDENTIAL_APP_ID $APP_CLIENT_ID -TENANT $TENANT

Write-Host "############## Step 9: Create Client Secret ##############" -ForegroundColor Yellow
$secretsConfig = .\setup-secrets.ps1 -RESOURCE_PREFIX $RESOURCE_PREFIX -APP_CLIENT_ID $APP_CLIENT_ID
$CLIENT_SECRET = $secretsConfig.ClientSecret
az webapp config appsettings set -n $WEB_APP_NAME -g $RESOURCE_GROUP_NAME --settings AuthConfig__ClientSecret=$CLIENT_SECRET --only-show-errors

Write-Host "############## Step 10: Generate appsettings.json ##############" -ForegroundColor Yellow
start-sleep -Milliseconds 500

$appsettings = @{
  AuthConfig = @{
    AppClientId = "$APP_CLIENT_ID"
    TenantId = "$TENANT"
    ManagedIdentityClientId = "$USER_ASSIGNED_CLIENT_ID"
    ClientSecret = "$CLIENT_SECRET"
    UseManagedIdentity = "false"
  }
  AzureStorageConfig = @{
    AccountName = "$STORAGE_ACCOUNT_NAME"
    ContainerName = "$CONTAINER_NAME"
  }
  KeyVaultConfig = @{
    Local = @{
      Uri = "https://$KEYVAULT_NAME.vault.azure.net/"
      SecretName="$SECRET_NAME"
    }
    Remote = @{
      Uri = ""
      SecretName=""
    }
  }
  Logging = @{
    LogLevel = @{
      Default = "Information"
      Microsoft = "Warning"
      "Microsoft.Hosting.Lifetime" = "Information"
    }
  }
  AllowedHosts = "*"
  MetadataOnly = @{
    WebAppUrl = "$WEB_APP_URL"
  }
}
$appsettingsJson = $appsettings | ConvertTo-Json -Depth 5
Set-Content -Path "..\appsettings.json" -Value $appsettingsJson
Write-Host "appsettings.json generated successfully!" -ForegroundColor Green

Write-Host "Environment setup complete!" -ForegroundColor Green

Write-Host "############## Step 11: Build the code and deploy the app ##############" -ForegroundColor Yellow
start-sleep -Milliseconds 500
.\deploy-server.ps1 -RESOURCE_PREFIX $RESOURCE_PREFIX

Write-Host "Deployment complete!" -ForegroundColor Green

start-sleep -Seconds 1

Write-Host @"
=============================================================================
          Setup Complete!
=============================================================================
"@