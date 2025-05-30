### **1. Azure Login and Subscription Setup**

The script ensures you are logged into Azure and sets the appropriate subscription for resource creation. If not logged in, the script prompts for credentials.

---

### **2. Create a Resource Group**

The script creates a resource group to organize all resources.

```powershell
az group create --name <ResourceGroupName> --location <Region>
```

---

### **3. Create a Storage Account and Container**
Provision a storage account and create a container for storing application data.

```powershell
az storage account create --name <StorageAccountName> --resource-group <ResourceGroupName> --location <Region> --sku Standard_LRS --kind StorageV2
az storage container create --name <ContainerName> --account-name <StorageAccountName>
```

---

### **4. Provision a Managed Identity**
A User Assigned Managed Identity is created to enable secure resource access.

```powershell
az identity create --name <ManagedIdentityName> --resource-group <ResourceGroupName> --location <Region>
```

---

### **5. Deploy App Service Plan and Web App**
The script creates an App Service Plan and a Web App, then assigns the Managed Identity to the Web App.

```powershell
az appservice plan create --name <AppPlanName> --resource-group <ResourceGroupName> --sku FREE
az webapp create --name <WebAppName> --resource-group <ResourceGroupName> --plan <AppPlanName>
az webapp identity assign --resource-group <ResourceGroupName> --name <WebAppName> --identities <ManagedIdentityReso
```

---

### **6. Register an Azure AD Application**
The application is registered in Azure AD to support authentication and permissions configuration.

```powershell
az ad app create --display-name <AppName> --sign-in-audience AzureADMultipleOrgs
```

---

### **7. Create an Azure Key Vault**
If needed, a Key Vault is provisioned, and secrets are created.

```powershell
az keyvault create --name <KeyVaultName> --resource-group <ResourceGroupName> --location <Region> --enable-rbac-authorization
az keyvault secret set --vault-name <KeyVaultName> --name <SecretName> --value "SecretValue"
```

---

### **8. Configure Federated Identity Credential**
The script creates a Federated Identity Credential to support modern authentication mechanisms.

```powershell
az ad app federated-credential create --id <AppId> --parameters fic-credential-config.json
```

---

### **9. Create client secret to compare the flows**
The script creates client secret for app authentication to resources. Used for comparison.

```powershell
$SECRET=$(az ad app credential reset --id <AppId> --append --display-name <SecretName> --years 1 --query "password" -o tsv)
```

---

### **10. Generate Application Configuration**
A JSON configuration file (appsettings.json) is generated for use with an ASP.NET Core application.

```json
{
  "AzureStorageConfig": {
    "ContainerName": "<container-name>",
    "AccountName": "<account-name>"
  },
  "AuthConfig": {
    "TenantId": "<tenant-id>",
    "ManagedIdentityClientId": "<managed-identity-id>",
    "ClientSecret": "<client-secret>",
    "AppClientId": "<app-id>"
  },
  "Logging": {
    "LogLevel": {
      "Microsoft.Hosting.Lifetime": "Information",
      "Microsoft": "Warning",
      "Default": "Information"
    }
  },
  "KeyVaultConfig": {
    "Uri": "<key-vault-url>",
    "SecretName": "<secret-name>"
  },
  "AllowedHosts": "*"
}
```

---

## **Step 11: Deploy the Application**
After resource provisioning, deploy your application code:

Build the application:

```powershell
dotnet publish --configuration Release --output ./publish
Compress the output:
```

Create a .zip archive to deploy on Azure.
```powershell
Compress-Archive -Path ./publish/* -DestinationPath ./package.zip -force
```

Deploy to the Web App:

```powershell
az webapp deploy --resource-group <ResourceGroupName> --name <WebAppName> --src-path ./package.zip --type zip
```

---

## **Step 12: Clean Up Resources**
After testing, clean up resources to avoid incurring charges:

```powershell
az group delete --name <ResourceGroupName> --yes --no-wait
```
---

