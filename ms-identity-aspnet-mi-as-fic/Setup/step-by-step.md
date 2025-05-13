## **Step 3: Step-by-Step Execution**

### **1. Azure Login and Subscription Setup**

The script ensures you are logged into Azure and sets the appropriate subscription for resource creation. If not logged in, the script prompts for credentials.

---

### **2. Create a Resource Group**

The script creates a resource group to organize all resources.

```bash
az group create --name <ResourceGroupName> --location <Region>
```
## **Step 3: Step-by-Step Execution**

### **1. Azure Login and Subscription Setup**

The script ensures you are logged into Azure and sets the appropriate subscription for resource creation. If not logged in, the script prompts for credentials.

---

### **2. Create a Resource Group**

The script creates a resource group to organize all resources.

```bash
az group create --name <ResourceGroupName> --location <Region>
```

---

## **Step 3: Step-by-Step Execution**

### **1. Azure Login and Subscription Setup**

The script ensures you are logged into Azure and sets the appropriate subscription for resource creation. If not logged in, the script prompts for credentials.

---

### **2. Create a Resource Group**

The script creates a resource group to organize all resources.

```bash
az group create --name <ResourceGroupName> --location <Region>
```

---

### **3. Create a Storage Account and Container**
Provision a storage account and create a container for storing application data.

```bash
az storage account create --name <StorageAccountName> --resource-group <ResourceGroupName> --location <Region> --sku Standard_LRS --kind StorageV2
az storage container create --name <ContainerName> --account-name <StorageAccountName>
```

---

### **4. Provision a Managed Identity**
A User Assigned Managed Identity is created to enable secure resource access.

```bash
az identity create --name <ManagedIdentityName> --resource-group <ResourceGroupName> --location <Region>
```

---

### **5. Deploy App Service Plan and Web App**
The script creates an App Service Plan and a Web App, then assigns the Managed Identity to the Web App.

```bash
az appservice plan create --name <AppPlanName> --resource-group <ResourceGroupName> --sku FREE
az webapp create --name <WebAppName> --resource-group <ResourceGroupName> --plan <AppPlanName>
az webapp identity assign --resource-group <ResourceGroupName> --name <WebAppName> --identities <ManagedIdentityReso
```

---

### **6. Register an Azure AD Application**
The application is registered in Azure AD to support authentication and permissions configuration.

```bash
az ad app create --display-name <AppName> --sign-in-audience AzureADMultipleOrgs
```

---

### **7. Optional: Create an Azure Key Vault**
If needed, a Key Vault is provisioned, and secrets are created.

```bash
az keyvault create --name <KeyVaultName> --resource-group <ResourceGroupName> --location <Region> --enable-rbac-authorization
az keyvault secret set --vault-name <KeyVaultName> --name <SecretName> --value "SecretValue"
```

---

### **8. Configure Federated Identity Credential**
The script creates a Federated Identity Credential to support modern authentication mechanisms.

```bash
az ad app federated-credential create --id <AppId> --parameters fic-credential-config.json
```

---

### **9. Generate Application Configuration**
A JSON configuration file (appsettings.json) is generated for use with an ASP.NET Core application.

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "<DomainName>",
    "TenantId": "<TenantId>",
    "ClientId": "<ClientId>",
    "CallbackPath": "/signin-oidc"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
```

---

## **Step 4: Deploy the Application**
After resource provisioning, deploy your application code:

Build the application:

```bash
dotnet publish --configuration Release --output ./publish
Compress the output:
```

Create a .zip archive to deploy on Azure.
```bash
Compress-Archive -Path ./publish/* -DestinationPath ./package.zip -force
```

Deploy to the Web App:

```bash

az webapp deployment source config-zip --resource-group <ResourceGroupName> --name <WebAppName> --src ./package.zip
```
---

## **Step 5: Clean Up Resources**
After testing, clean up resources to avoid incurring charges:

```bash
az group delete --name <ResourceGroupName> --yes --no-wait
```
---

