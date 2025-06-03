# =======================================================================================
# setup-secrets.ps1
# =======================================================================================
# This script creates auth secrets to show the traditional way to auth 
#
# Usage:
#   .\setup-secrets.ps1 -RESOURCE_PREFIX <prefix> -APP_CLIENT_ID <app-id>
# =======================================================================================

param (    
    [Parameter(Mandatory=$True, HelpMessage='Resource Prefix used for naming the resources')]
    [string]$RESOURCE_PREFIX,

    [Parameter(Mandatory=$True)]
    [string]$APP_CLIENT_ID
)


# Create secret
$SECRET_NAME = $RESOURCE_PREFIX + "2Secret"
$SECRET=$(az ad app credential reset --id $APP_CLIENT_ID --append --display-name $SECRET_NAME --years 1 --query "password" -o tsv --only-show-errors)

return @{
    ClientSecret = $SECRET
}