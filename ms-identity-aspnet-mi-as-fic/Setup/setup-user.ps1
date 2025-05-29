# ========================================================================
# setup-user.ps1
# ========================================================================
# This script logs in the user
#
# Usage:
#   .\setup-user.ps1 -TENANT <tenant-id> -SUBSCRIPTION <subscription-id>
# ========================================================================

[CmdletBinding()]
param (
    [Parameter(Mandatory=$False)]
    [string]$TENANT,

    [Parameter(Mandatory=$False)]
    [string]$SUBSCRIPTION
)

function Get-UserSelection {
    param (
        [Parameter(Mandatory=$True)]
        [array]$Items,
        
        [Parameter(Mandatory=$True)]
        [string]$Prompt
    )
    
    Write-Host "`n$Prompt" -ForegroundColor Yellow
    for ($i = 0; $i -lt $Items.Count; $i++) {
        Write-Host "$($i + 1). $($Items[$i])"
    }
    
    $selection = Read-Host "Select a number (1-$($Items.Count))"
    while (-not ($selection -match "^\d+$" -and [int]$selection -ge 1 -and [int]$selection -le $Items.Count)) {
        $selection = Read-Host "Please enter a valid number (1-$($Items.Count))"
    }
    
    return $Items[[int]$selection - 1]
}

# Check if user is logged in
$accounts = az account list --query "[].{name:name, id:id, tenantId:tenantId}" -o json --only-show-errors | ConvertFrom-Json 

if (-not $accounts) {
    Write-Host "No active Azure login found. Please log in..." -ForegroundColor Yellow
    
    $loginResult = az login 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Azure login failed. Exiting script."
        exit 1
    }

    $accounts = az account list --query "[].{name:name, id:id, tenantId:tenantId}" -o json | ConvertFrom-Json
}

# Handle multiple accounts
if ($accounts.Count -gt 1) {
    $accountNames = $accounts | ForEach-Object { "$($_.name) (Tenant: $($_.tenantId))" }
    $selectedAccount = Get-UserSelection -Items $accountNames -Prompt "Multiple accounts found. Please select one:"
    $selectedIndex = [array]::IndexOf($accountNames, $selectedAccount)
    $SUBSCRIPTION = $accounts[$selectedIndex].id
    $TENANT = $accounts[$selectedIndex].tenantId
}
else {
    $SUBSCRIPTION = $accounts[0].id
    $TENANT = $accounts[0].tenantId
}

# Set the subscription
az account set --subscription $SUBSCRIPTION

# Get current user info
$USER_EMAIL = az ad signed-in-user show --query "userPrincipalName" -o tsv
$DOMAIN_NAME = $USER_EMAIL.Split('@')[1]
$CURRENT_USER_OBJECT_ID = $(az ad signed-in-user show --query id -o tsv)

Write-Host "`nWelcome: $USER_EMAIL!" -ForegroundColor Cyan
Write-Host "Using subscription: $($accounts | Where-Object { $_.id -eq $SUBSCRIPTION } | Select-Object -ExpandProperty name)" -ForegroundColor Cyan
Write-Host "Using tenant: $TENANT" -ForegroundColor Cyan
Start-Sleep -Milliseconds 300

# Return user information
@{
    UserEmail = $USER_EMAIL
    DomainName = $DOMAIN_NAME
    CurrentUserObjectId = $CURRENT_USER_OBJECT_ID
    Subscription = $SUBSCRIPTION
    Tenant = $TENANT
} 