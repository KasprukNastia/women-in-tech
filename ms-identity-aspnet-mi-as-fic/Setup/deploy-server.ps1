# =============================================================================
# deploy-server.ps1
# =============================================================================
# This script builds and deploys the MiFicExamples to Azure Web App
#
# Usage:
#   .\deploy-server.ps1 -RESOURCE_PREFIX <prefix>
# =============================================================================

[CmdletBinding()]
param (
    [Parameter(Mandatory=$True, HelpMessage='Prefix used for resource naming')]
    [string]$RESOURCE_PREFIX
)

$RESOURCE_GROUP_NAME = $RESOURCE_PREFIX + "2RG"
# Define resource names
$WEB_APP_NAME = $RESOURCE_PREFIX + "2WebApp"

Write-Host "Building and deploying the application..." -ForegroundColor Yellow

# Build the application
Write-Host "Building MiFicExamples..." -ForegroundColor Yellow
dotnet publish ..\MiFicExamples.csproj --configuration Release --output ./publish

# Create deployment package
Write-Host "Creating deployment package..." -ForegroundColor Yellow
Compress-Archive -Path ./publish/* -DestinationPath ./package.zip -Force

# Deploy to Azure Web App
Write-Host "Deploying to Azure Web App..." -ForegroundColor Yellow
az webapp deploy --resource-group $RESOURCE_GROUP_NAME --name $WEB_APP_NAME --src-path ./package.zip --type zip

# Clean up temporary files
Write-Host "Cleaning up temporary files..." -ForegroundColor Yellow
Remove-Item -Path ./publish -Recurse -Force
Remove-Item -Path ./package.zip -Force

Write-Host "Deployment of Web App '$WEB_APP_NAME' complete!" -ForegroundColor Green 