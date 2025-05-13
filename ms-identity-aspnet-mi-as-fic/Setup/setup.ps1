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
7. Optionally creates an Azure Key Vault in the same or a different tenant and sets up access roles and a secret.
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
- RESOURCE_PREFIX: A prefix for naming Azure resources (optional).
- LOCATION: The Azure region where resources will be created (optional).
- REMOTE_KV_TENANT: The Tenant ID for the Key Vault if it needs to be created in a different tenant.
- REMOTE_KV_SUBSCRIPTION: The Subscription ID for the Key Vault in a different tenant.

.EXAMPLE
.\setup.ps1 -RESOURCE_PREFIX "TPO1" -LOCATION northeurope

.EXAMPLE
.\setup.ps1 -RESOURCE_PREFIX "TPO1" -LOCATION northeurope -REMOTE_KV_TENANT a4604de3-e541-455b-8429-f53850a7c237 -REMOTE_KV_SUBSCRIPTION c8802953-fac5-44d5-a743-95e3f3a46c6f

#>

[CmdletBinding()]
param (
    [Parameter(Mandatory=$False, HelpMessage='A prefix that will be used to name the resources')]
    [string]$RESOURCE_PREFIX,

    [Parameter(Mandatory=$False, HelpMessage='The Azure location where the resources will be created')]
    [string]$LOCATION,

    [Parameter(Mandatory=$True, HelpMessage='A different tenant to create the Key Vault in. Will create it in the home app tenant if not provided')]
    [string]$REMOTE_KV_TENANT,

    [Parameter(Mandatory=$True, HelpMessage='The subscription to create the keyvault in. Will create it in the home app tenant if not provided')]
    [string]$REMOTE_KV_SUBSCRIPTION
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
$CURRENT_USER_OBJECT_ID = $userConfig.CurrentUserObjectId
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

Write-Host "############## Step 6: Create App Registration ##############" -ForegroundColor Yellow
start-sleep -Milliseconds 500
$confedentialAppConfig = .\setup-confidential-app.ps1 -RESOURCE_PREFIX $RESOURCE_PREFIX -WEB_APP_URL $WEB_APP_URL
$APP_CLIENT_ID = $confedentialAppConfig.ConfidentialAppId
$APP_REG_NAME = $confedentialAppConfig.AppRegistrationName

Write-Host "Assigning Storage Blob Data Contributor role to the app.." -ForegroundColor Yellow
$SP_OBJECT_ID = $(az ad sp show --id $APP_CLIENT_ID --query id -o tsv)
az role assignment create --assignee $SP_OBJECT_ID --role "Storage Blob Data Contributor" --scope "/subscriptions/$SUBSCRIPTION/resourceGroups/$RESOURCE_GROUP_NAME/providers/Microsoft.Storage/storageAccounts/$STORAGE_ACCOUNT_NAME"

Write-Host "############## Step 7: Remote Key Vault Creation ##############" -ForegroundColor Yellow
start-sleep -Milliseconds 500
$REMOTE_USER_EMAIL = ""
$REMOTE_SP_OBJECT_ID = ""

if (-not $REMOTE_KV_TENANT -or -not $REMOTE_KV_SUBSCRIPTION) {
    Write-Host "You have not provided a remote subscription for keyvault. Let's create it in the current subscription" -ForegroundColor Yellow
    $REMOTE_USER_EMAIL = $CURRENT_USER_EMAIL
    $REMOTE_SP_OBJECT_ID = $SP_OBJECT_ID
}
else {
    Write-Host "Now creating a Key Vault in another tenant.. Please be ready to login to the other tenant." -ForegroundColor Yellow
    Start-Sleep -Milliseconds 500
    $userConfigRemote = .\setup-user.ps1 -TENANT $REMOTE_KV_TENANT -SUBSCRIPTION $REMOTE_KV_SUBSCRIPTION
    $REMOTE_USER_EMAIL = $userConfigRemote.UserEmail
    $REMOTE_DOMAIN_NAME = $userConfigRemote.DomainName
    Write-Host "$REMOTE_USER_EMAIL logged in successfully to $REMOTE_DOMAIN_NAME!" -ForegroundColor Cyan
    Start-Sleep -Milliseconds 100
}

$keyVaultConfig = .\setup-key-vault.ps1 -RESOURCE_PREFIX $RESOURCE_PREFIX -SUBSCRIPTION $SUBSCRIPTION -LOCATION $LOCATION -USER_EMAIL $REMOTE_USER_EMAIL -SP_OBJECT_ID $REMOTE_SP_OBJECT_ID
$KEYVAULT_NAME = $keyVaultConfig.KeyVaultName
$SECRET_NAME = $keyVaultConfig.SecretName

Write-Host "############## Step 8: Create Federated Identity Credential ##############" -ForegroundColor Yellow
# Make sure the user is logged in to the correct tenant
az account set --subscription $SUBSCRIPTION
$ficConfig = .\setup-fic.ps1 -RESOURCE_PREFIX $RESOURCE_PREFIX -CONFEDENTIAL_APP_ID $APP_CLIENT_ID -TENANT $TENANT

Write-Host "############## Step 9: Generate appsettings.json ##############" -ForegroundColor Yellow
start-sleep -Milliseconds 500
# make sure the REMOTE_KV_TENANT is set
if (-not $REMOTE_KV_TENANT) {
    $REMOTE_KV_TENANT = $TENANT
}

$appsettings = @{
    AzureAd = @{
        Instance = "https://login.microsoftonline.com/"
        Domain = "$DOMAIN_NAME"
        TenantId = "$TENANT"
        ClientId = "$APP_CLIENT_ID"
        CallbackPath = "/signin-oidc"
        ClientCredentials = @(
            @{
                SourceType = "SignedAssertionFromManagedIdentity"
                ManagedIdentityClientId = "$USER_ASSIGNED_CLIENT_ID"
                TokenExchangeUrl = "api://AzureADTokenExchange"
            }
        )
    }
    DownstreamApis = @{
        MicrosoftGraph = @{
            BaseUrl = "https://graph.microsoft.com/v1.0"
            RequestAppToken = $false
            Scopes = @("User.Read")
        }
    }
    AzureStorageConfig = @{
        AccountName = "$STORAGE_ACCOUNT_NAME"
        ContainerName = "$CONTAINER_NAME"
    }
    KeyVault = @{
        TenantId = "$REMOTE_KV_TENANT"
        Uri = "https://$KEYVAULT_NAME.vault.azure.net/"
        SecretName="$SECRET_NAME"
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
        WebAppName = "$WEB_APP_NAME"
        WebAppUrl = "$WEB_APP_URL"
        $APP_REG_NAME = "$APP_REG_NAME"
        StorageAccountName = "$STORAGE_ACCOUNT_NAME"
        ContainerName = "$CONTAINER_NAME"
        ManagedIdentityName = "$MANAGED_IDENTITY_NAME"
        FicIssuer = "https://login.microsoftonline.com/$Tenant/v2.0"
        RemoteKeyVaultTenantAdminConsentUrl = "https://login.microsoftonline.com/$REMOTE_KV_TENANT/adminconsent?client_id=$APP_CLIENT_ID"        
    }
}
$appsettingsJson = $appsettings | ConvertTo-Json -Depth 3
Set-Content -Path "..\appsettings.json" -Value $appsettingsJson
Write-Host $appsettingsJson -ForegroundColor Green
Write-Host "appsettings.json generated successfully!" -ForegroundColor Green

Write-Host "Environment setup complete!" -ForegroundColor Green

Write-Host "############## Step :10 build the code and deploy the app ##############" -ForegroundColor Yellow
start-sleep -Milliseconds 500
.\deploy-server.ps1 -RESOURCE_PREFIX $RESOURCE_PREFIX

Write-Host "Deployment complete!" -ForegroundColor Green

start-sleep -Seconds 1

Write-Host @"
=============================================================================
                    Setup Complete!
=============================================================================
Your Code Critic application has been set up with the following components:

1. Azure AD Applications:
   - Confidential Client (API): $CONFEDENTIAL_APP_ID
   - Public Client: $PUBLIC_APP_ID
To view the App Registration, visit: https://portal.azure.com/#blade/Microsoft_AAD_RegisteredApps/ApplicationsListBlade

2. Storage Account:
   - Name: $STORAGE_ACCOUNT_NAME
   - Container: $CONTAINER_NAME
To view the Storage Account, visit: https://portal.azure.com/#resource/subscriptions/$SUBSCRIPTION/resourceGroups/$RESOURCE_GROUP_NAME/providers/Microsoft.Storage/storageAccounts/$STORAGE_ACCOUNT_NAME
   
3. Key Vault:
   - Name: $KEYVAULT_NAME
   - Secret: $SECRET_NAME
   - KV Tenant: $REMOTE_KV_TENANT
To view the Key Vault, visit: https://portal.azure.com/#resource/subscriptions/$REMOTE_KV_SUBSCRIPTION/resourceGroups/$RESOURCE_GROUP_NAME/providers/Microsoft.KeyVault/vaults/$KEYVAULT_NAME

4. Web App:
   - URL: $WEB_APP_URL
   - Managed Identity: $managedIdentityConfig.ManagedIdentityName
To view the web app, visit: $WEB_APP_URL
To view logs, run: az webapp log tail --name $WEB_APP_NAME --resource-group $RESOURCE_GROUP_NAME

If you created the Key Vault in a different tenant, please visit the following URL to grant admin consent:
https://login.microsoftonline.com/$REMOTE_KV_TENANT/adminconsent?client_id=$APP_CLIENT_ID
=============================================================================
"@