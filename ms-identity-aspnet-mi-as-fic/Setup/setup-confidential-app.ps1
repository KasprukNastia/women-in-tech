[CmdletBinding()]
param (
    [Parameter(Mandatory=$True)]
    [string]$RESOURCE_PREFIX,

    [Parameter(Mandatory=$True)]
    [string]$WEB_APP_URL
)

Write-Host "Creating App Registration (Confidential Client)..." -ForegroundColor Yellow
$CONFEDENTIAL_APP_NAME = $RESOURCE_PREFIX + "2ConfidentialAppReg"

# Check if app already exists
$existingApp = az ad app list --display-name $CONFEDENTIAL_APP_NAME --query "[].{appId:appId}" -o tsv
if ($existingApp) {
    Write-Host "App already exists, using existing app..." -ForegroundColor Yellow
    $CONFEDENTIAL_APP_ID = $existingApp
} else {
    # Create new app
    $appResult = az ad app create --display-name $CONFEDENTIAL_APP_NAME --sign-in-audience "AzureADandPersonalMicrosoftAccount"
    if (-not $?) {
        Write-Host "Failed to create app registration" -ForegroundColor Red
        exit 1
    }
    $CONFEDENTIAL_APP_ID = $(az ad app list --display-name $CONFEDENTIAL_APP_NAME --query "[].{appId:appId}" -o tsv)
}

# Check if service principal exists
$existingSp = az ad sp list --filter "appId eq '$CONFEDENTIAL_APP_ID'" --query "[].{id:id}" -o tsv
if (-not $existingSp) {
    # Create service principal and wait for it to be ready
    az ad sp create --id $CONFEDENTIAL_APP_ID
    if (-not $?) {
        Write-Host "Failed to create service principal" -ForegroundColor Red
        exit 1
    }
    Start-Sleep -Seconds 3
}


Write-Host "Updating web app redirect URIs..." -ForegroundColor Yellow

# Update web app redirect URIs
az ad app update --id $CONFEDENTIAL_APP_ID --web-redirect-uris "$WEB_APP_URL/signin-oidc"
if (-not $?) {
    Write-Host "Failed to update redirect URIs" -ForegroundColor Red
    exit 1
}

# Get app object ID
$APP_OBJECT_ID = $(az ad app show --id $CONFEDENTIAL_APP_ID --query id -o tsv)
if (-not $APP_OBJECT_ID) {
    Write-Host "Failed to retrieve app object ID" -ForegroundColor Red
    exit 1
}

Write-Host "Enabling ID token issuance..." -ForegroundColor Yellow
# Enable ID token issuance
$implicitGrantSettings = @{
    web = @{
        implicitGrantSettings = @{
            EnableIdTokenIssuance = $true
        }
    }
} | ConvertTo-Json -Depth 10

$implicitGrantSettings | Out-File -FilePath "implicit-grant.json" -Force
az rest --method PATCH --uri "https://graph.microsoft.com/v1.0/applications/$APP_OBJECT_ID" --headers 'Content-Type=application/json' --body "@implicit-grant.json"
if (-not $?) {
    Write-Host "Failed to enable ID token issuance" -ForegroundColor Red
    Remove-Item -Path "implicit-grant.json" -Force
    exit 1
}
Remove-Item -Path "implicit-grant.json" -Force

Write-Host "Creating optional claims manifest..." -ForegroundColor Yellow
# Create optional claims manifest
$optionalClaims = @{
    idToken = @(
        @{
            name = "email"
            source = $null
            essential = $false
            additionalProperties = @()
        }
    )
}
$optionalClaims | ConvertTo-Json -Depth 10 | Out-File -FilePath "optional-claims-manifest.json" -Force

# Include the email claim in the ID token
az ad app update --set optionalClaims=@optional-claims-manifest.json --id $CONFEDENTIAL_APP_ID
if (-not $?) {
    Write-Host "Failed to update optional claims" -ForegroundColor Red
    Remove-Item -Path "optional-claims-manifest.json" -Force
    exit 1
}
Remove-Item -Path "optional-claims-manifest.json" -Force

Write-Host "Assigning MS Graph permission..." -ForegroundColor Yellow
# Assign MS Graph permission
$graphPermission = az ad app permission list --id $CONFEDENTIAL_APP_ID --query "[?resourceAppId=='00000003-0000-0000-c000-000000000000']" -o json | ConvertFrom-Json
if (-not $graphPermission -or $graphPermission.Count -eq 0) {
    az ad app permission add --id $CONFEDENTIAL_APP_ID --api "00000003-0000-0000-c000-000000000000" --api-permissions "e1fe6dd8-ba31-4d61-89e7-88639da4683d=Scope"
    if (-not $?) {
        Write-Host "Failed to add MS Graph permission" -ForegroundColor Red
        exit 1
    }
}

# Wait for permissions to be added
Start-Sleep -Seconds 3

Write-Host "Granting admin consent for MS Graph..." -ForegroundColor Yellow
# Grant admin consent for the app permissions
$graphGrant = az ad app permission list-grants --id $CONFEDENTIAL_APP_ID --query "[?resourceAppId=='00000003-0000-0000-c000-000000000000']" -o json | ConvertFrom-Json
if (-not $graphGrant -or $graphGrant.Count -eq 0) {
    az ad app permission grant --id $CONFEDENTIAL_APP_ID --api "00000003-0000-0000-c000-000000000000" --scope "e1fe6dd8-ba31-4d61-89e7-88639da4683d"
    if (-not $?) {
        Write-Host "Failed to grant admin consent for MS Graph" -ForegroundColor Red
        exit 1
    }
}

# Wait for permissions to be added
Start-Sleep -Seconds 3

# Try to grant admin consent, but don't fail if it doesn't work
Write-Host "Attempting to grant admin consent..." -ForegroundColor Yellow
try {
    az ad app permission admin-consent --id $CONFEDENTIAL_APP_ID
    if (-not $?) {
        Write-Host "Warning: Could not grant admin consent automatically. You may need to grant admin consent manually." -ForegroundColor Yellow
        Write-Host "To grant admin consent manually, visit:" -ForegroundColor Yellow
        Write-Host "https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/~/CallAnAPI/appId/$CONFEDENTIAL_APP_ID" -ForegroundColor Cyan
    }
} catch {
    Write-Host "Warning: Could not grant admin consent automatically. You may need to grant admin consent manually." -ForegroundColor Yellow
    Write-Host "To grant admin consent manually, visit:" -ForegroundColor Yellow
    Write-Host "https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/~/CallAnAPI/appId/$CONFEDENTIAL_APP_ID" -ForegroundColor Cyan
}


# Return values needed by other scripts
@{
    ConfidentialAppId = $CONFEDENTIAL_APP_ID
    AppRegistrationName = $CONFEDENTIAL_APP_NAME
} 