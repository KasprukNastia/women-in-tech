# Managed Identity as Federated Credential – Implementation guide and code snippets
## Overview
Using a Managed Identity as a Federated Credential means an Azure resource's managed identity can serve as the authentication credential for a Microsoft Entra application (Azure AD app registration). Instead of a client secret or certificate, the app trusts a token issued to a managed identity. At runtime, the workload obtains an access token via its managed identity, then exchanges that token for an application access token to call target resources​. This approach is part of Workload Identity Federation, allowing managed identities to act "like a password or certificate" for Entra apps​.

We'll provide more details on the setup and code snippets in several programming languages in this article.

### Why use it
- No secrets: No need to store or rotate app secrets or certificates.
- Secure by default: Managed identity credentials can only be retrieved from the Azure environment hosting the workload.
- Cross-tenant access: Multitenant apps can access resources across tenants using a managed identity credential, without passwords! This enables scenarios like BYOK for Storage, SQL, Cosmos.
- Secretless migration path: Apps using client secrets can migrate to this model with minimal change.

## Implementation

>Note:
also review the official guidance: [Configure an application to trust a managed identity](https://learn.microsoft.com/en-us/entra/workload-id/workload-identity-federation-config-app-trust-managed-identity)


Here are the general steps involved:
1. Create a Microsoft Entra application registration for your service (or use an existing multi-tenant app if appropriate). When creating, you can choose "Accounts in this organizational directory only" or make it multi-tenant depending on needs.
2. Do not create any client secret or certificate. (you might need one for local devbox development).
3. Assign a managed identity in the Azure environment where your code will run (App Service, VM, container instances, etc.). You need a user-assigned identity for this feature.
4. Configure the Entra app with a Federated Identity Credential that trusts the managed identity's tokens (details in next section). This establishes the trust relationship.
5. Grant the Entra app appropriate the needed API permissions and role assignments to target resources.
6. Implement the code using the token exchange flow (see Code Examples for .NET, Node.js, Python, Java, and Go.) by requesting an access token for the managed identity - with the token exchange audience - and exchange it for an app token to call the target resource. No secrets are involved in the code or config.

### Enabling Managed Identity on Azure Services (Environment Setup)

Before configuring the Entra app, ensure your application's host environment in Azure has a managed identity available:

#### Example: App services
In Azure App Service (& Azure Functions), you can enable a managed identity via the Azure portal or CLI:

- **Portal**: Navigate to your Web App or Function App in the Azure Portal. Under Settings, select Identity. Add a User-assigned identity (or select from your existing identities).

- **Azure CLI**: Use the CLI for automation. For example, to assign an existing user-assigned identity:

```bash
az webapp identity assign --resource-group <YourResourceGroup> --name <YourAppName> --identities <IdentityResourceID>
```

(For Function Apps, use az functionapp identity assign similarly.)

After enabling, note the managed identity's identifiers. In the portal, the Object ID (also called Principal ID) and Client ID will be displayed in the Identity blade. The Object ID is the GUID used to identify this identity in access control and in the federated credential setup (it will be used as the subject in the Entra app's federated credential). The Client ID is used by code to explicitly reference this identity (especially if using a user-assigned MI or if multiple identities are present).

### Configuring the Entra application

Next, configure the Microsoft Entra ID application to trust the managed identity's tokens:

1. **Application registration**: If you haven't already, register an application in Entra ID (or Azure) or locate your existing app registration. This app represents your service. Make sure you have the Application's Client ID handy. If you plan a cross-tenant scenario, you must set the app to be multi-tenant (accounts in any organizational directory) so it can be used in other tenants​. The app must belong to the same tenant as the managed identity for the trust to be established​. It is important to note that in all cases, single tenant and  cross-tenant, the app must be registered in the same source tenant along with the managed identity. In multitenant, the app need to be provisioned and granted access on resources in the target tenants.

2. **Permissions and API Access**: Assign any required API permissions or roles to this app. For instance, if the app will access resources or call APIs on behalf of the user, for example read their profile from Microsoft Graph, you need to add the necessary Application API permissions and grant admin consent. If app is directly accessing Azure resources like Key Vault or Storage, you will need to grant the app access at the resource level (e.g. a Key Vault access policy or RBAC role for the app's principal).

3. **Add a Federated Identity Credential**: This is the key step that links the managed identity to the app:

    - **Using Azure Portal**: In the Entra admin center (Azure AD portal), go to App Registrations > Your App > Certificates & secrets > Federated credentials (tab). Click Add credential. In the "Federated credential scenario" dropdown, select Managed Identity (if available). You will be prompted to choose the managed identity. Select the subscription and the specific User-assigned managed identity resource that you enabled earlier​. The portal will auto-fill the Issuer, Subject, and Audience for you.

    - **Using PowerShell**: You can script this for automation. Ensure you have the Azure PowerShell Az module and run a command like:

    ```powershell
    $appObjectId = "<YOUR_APP_OBJECT_ID>"
    New-AzADAppFederatedCredential -ApplicationObjectId $appObjectId `
        -Name "MyManagedIdFederation" `
        -Issuer "https://login.microsoftonline.com/<TENANT_ID>/v2.0" `
        -Subject "<MANAGED_IDENTITY_OBJECT_ID>" `
        -Audience "api://AzureADTokenExchange"
    ```

    This will create a federated credential on the app with the given issuer (your tenant's Azure AD issuer URL) and subject (the managed identity's principal ID)​. The audience is set to api://AzureADTokenExchange for the global Azure cloud (see below for other clouds). The Name is an arbitrary descriptor for the credential.

    - **Using Azure CLI**: As of now, Azure CLI may require using the Graph REST API to add a federated credential. For example:

    ```bash
    az ad app federated-credential create --id <YOUR_APP_OBJECT_ID> --parameters credential.json
    ```
    This is an example json file defining the credentials:
    ```json
    {
    "name": "msi-webapp1",
    "issuer": "https://login.microsoftonline.com/{TENANT_ID}/v2.0",
    "subject": "{MANAGED_IDENTITY_OBJECT_ID}",
    "description": "Trust the workload's UAMI to impersonate the App",
    "audiences": [
        "api://AzureADTokenExchange"
        ]
    }
    ```
    
    - **Using Graph APIs**: 
    ```bash
    az rest --method POST \
      --uri "https://graph.microsoft.com/v1.0/applications/<APP_OBJECT_ID>/federatedIdentityCredentials" \
      --body '{
        "name": "MyManagedIdFederation",
        "issuer": "https://login.microsoftonline.com/<TENANT_ID>/v2.0",
        "subject": "<MANAGED_IDENTITY_OBJECT_ID>",
        "audiences": ["api://AzureADTokenExchange"]
      }'
    ```
   
4. When creating the FICs, ensure the Issuer value is exactly your tenant's token issuer(https://login.microsoftonline.com/{tenantId}/v2.0). The Subject is the managed identity's object ID (a GUID). The Audience must be the special fixed value api://AzureADTokenExchange (for public Azure). In sovereign clouds, use the corresponding audience:

    - Azure public (Entra ID global service): 
        - issuer: https://login.microsoftonline.com/{tenantId}/v2.0
        - api://AzureADTokenExchange
    - Azure Government (Entra ID for US Government): 
        - issuer: https://login.microsoftonline.us/{tenantId}/v2.0
        - api://AzureADTokenExchangeUSGov
    - Azure China (Entra China operated by 21Vianet): 
        - issuer: https://login.microsoftonline.cn/{tenantId}/v2.0
        - api://AzureADTokenExchangeChina

5. **Roles and access for resources**: Now that the app can obtain a token, make sure the app is authorized to access the target resource:

    - For direct app permissions on Azure resource access (e.g., Key Vault, Storage, Cosmos DB, etc.), treat the Entra app like a service principal. If the resource is in the same tenant, the app registration itself is the service principal; if the resource is in a different tenant, the app's service principal must exist in that tenant (by an admin consent to provision the app's in the target tenant)​. Then assign appropriate RBAC roles or access policies to that principal on the resource. For example, assign the app the Key Vault Secrets User or a custom access policy on a Key Vault, or a Storage Blob Data Reader role on a storage account.
    - For delegated permissions, e.g. to access Microsoft Graph or other APIs on behalf of the signed-in user: ensure the app registration has the needed API permissions with admin consent, or the relevant delegated permissions if a user context were involved (usually for service-to-service you use application perms). Without this, even if you get a token, the API call might be unauthorized.

At this point, the Azure environment is set up: your Azure resource has a managed identity, and your Entra application trusts that identity's tokens. Next, you will implement the code in your application to use this setup.

### Acquiring access tokens from code

With the above configuration in place, the application's code can obtain tokens as follows:
1. Fetch an access token using the managed identity (from the local environment – e.g., using Azure SDK).
2. Use that token as a client assertion to authenticate as the Entra application and acquire a new token for the target resource.
3. Use the resulting token to access the resource (e.g., call an Azure service REST API or SDK with the token).

Below are concise code examples in several languages demonstrating this flow. Each example shows obtaining a token for a resource (Key Vault or Storage) using a managed identity and exchanging it for an application token. These assume you have configured the values as described earlier (client IDs, tenant IDs, etc.). Replace the placeholder values (YOUR_APP_CLIENT_ID, etc.) with your actual IDs.

### .NET (C#)
The .NET example uses the `Azure.Identity` library to authenticate to Azure Blob Storage using a managed identity as a federated credential. It creates a ManagedIdentityCredential to get the initial token, wraps it in a ClientAssertionCredential (which uses the managed identity token as a signed assertion for the app), and then uses the Azure Storage SDK with that credential to list blob items.

In this example, the `BlobContainerClient` internally uses the provided assertionCredential to obtain an access token for Azure Storage (scope https://<storage_account>.blob.core.windows.net/.default). The managed identity token is exchanged behind the scenes for the app's token. The code works for same-tenant or cross-tenant (if resourceTenantId is different, it will request the token in that tenant). Make sure the Entra app is granted access to the storage container (e.g., via a role assignment) or the call will be unauthorized.

```csharp
using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

string storageAccountName = "YOUR_STORAGE_ACCOUNT_NAME";
string containerName     = "YOUR_CONTAINER_NAME";

// Application (client) ID of your Entra app registration
string appClientId = "YOUR_APP_CLIENT_ID";

// Tenant ID where the target resource resides (could be same as app's tenant)
string resourceTenantId = "YOUR_RESOURCE_TENANT_ID";

// Client ID of the managed identity
string managedIdentityClientId = "YOUR_MANAGED_IDENTITY_CLIENT_ID";

// Audience for token exchange (use cloud-specific value if not in public Azure)
string audience = "api://AzureADTokenExchange";

// 1. Acquire a managed identity token for the "AzureADTokenExchange" audience.
var miCredential = new ManagedIdentityCredential(managedIdentityClientId);
var assertionCredential = new ClientAssertionCredential(
    resourceTenantId,
    appClientId,
    async (_) => {
        // This callback is invoked to get the client assertion (MI token)
        var context = new TokenRequestContext(new[] { $"{audience}/.default" });
        var accessToken = await miCredential.GetTokenAsync(context).ConfigureAwait(false);
        return accessToken.Token;
    });

// 2. Use the ClientAssertionCredential (which presents the MI token as an assertion) to create a client for the resource.
BlobContainerClient containerClient = new BlobContainerClient(
    new Uri($"https://{storageAccountName}.blob.core.windows.net/{containerName}"),
    assertionCredential);

// 3. Use the BlobContainerClient as usual – the SDK will use the provided credential to authenticate.
await foreach (BlobItem blob in containerClient.GetBlobsAsync())
{
    Console.WriteLine($"Blob name: {blob.Name}");
    // (Perform operations with blobClient if needed)
}
```

### Node.js
The Node.js example uses the official Azure Identity package `@azure/identity` along with the Azure Key Vault Secrets client. It obtains a token from a managed identity and uses it as a client assertion via the `ClientAssertionCredential` class.


```javascript
const { ManagedIdentityCredential, ClientAssertionCredential } = require("@azure/identity");
const { BlobServiceClient } = require("@azure/storage-blob");

const STORAGE_ACCOUNT_NAME = "YOUR_STORAGE_ACCOUNT_NAME";
const CONTAINER_NAME = "YOUR_CONTAINER_NAME";
const APP_CLIENT_ID = "YOUR_APP_CLIENT_ID";
const RESOURCE_TENANT_ID = "YOUR_RESOURCE_TENANT_ID";
const MI_CLIENT_ID = "YOUR_MANAGED_IDENTITY_CLIENT_ID";
const AUDIENCE = "api://AzureADTokenExchange";

async function getAccessToken(credential, audience) {
    const token = await credential.getToken(audience + "/.default");
    if (!token || !token.token) {
        throw new Error("Failed to obtain managed identity token");
    }
    return token.token;
}

async function main() {
    // 1. Get managed identity token for token exchange audience
    const managedIdentityCredential = new ManagedIdentityCredential(MI_CLIENT_ID);

    // 2. Create a client assertion credential for the app using the managed identity token
    const clientAssertionCredential = new ClientAssertionCredential(
        RESOURCE_TENANT_ID,
        APP_CLIENT_ID,
        () => getAccessToken(managedIdentityCredential, AUDIENCE)
    );

    // 3. Use the credential to access Blob Storage
    const blobServiceClient = new BlobServiceClient(
        `https://${STORAGE_ACCOUNT_NAME}.blob.core.windows.net`,
        clientAssertionCredential
    );

    const containerClient = blobServiceClient.getContainerClient(CONTAINER_NAME);

    // 4. List blobs in the container
    console.log(`Listing blobs in container: ${CONTAINER_NAME}`);
    for await (const blob of containerClient.listBlobsFlat()) {
        console.log(`Blob: ${blob.name}`);
    }
}

main().catch(err => {
    console.error("Error accessing Blob Storage:", err);
});
```

### Python

In Python, the `azure.identity` library provides similar capabilities. Below, we use `ManagedIdentityCredential` and `ClientAssertionCredential` to get a Key Vault secret.

This script demonstrates how to access an Azure Blob Storage container using a managed identity token exchanged as a federated credential for a Microsoft Entra application. It retrieves a list of blobs from the container using `azure.identity` and `azure.storage.blob`.

> Note: Microsoft Authentication Library (MSAL) support for managed identity is not yet built into MSAL Python. The recommended approach is to use the `Azure Identity` library as shown below.

```python
from azure.identity import ManagedIdentityCredential, ClientAssertionCredential
from azure.storage.blob import BlobServiceClient

# Configurable values
APP_CLIENT_ID      = "YOUR_APP_CLIENT_ID"
RESOURCE_TENANT_ID = "YOUR_RESOURCE_TENANT_ID"
MI_CLIENT_ID       = "YOUR_MANAGED_IDENTITY_CLIENT_ID" 
AUDIENCE           = "api://AzureADTokenExchange"
STORAGE_ACCOUNT    = "YOUR_STORAGE_ACCOUNT_NAME"
CONTAINER_NAME     = "YOUR_CONTAINER_NAME"

# 1. Get a managed identity token for the token exchange audience
mi_credential = ManagedIdentityCredential(client_id=MI_CLIENT_ID)
def get_mi_token():
    token = mi_credential.get_token(f"{AUDIENCE}/.default")
    return token.token

# 2. Create a client assertion credential using the managed identity token
credential = ClientAssertionCredential(
    tenant_id=RESOURCE_TENANT_ID,
    client_id=APP_CLIENT_ID,
    client_assertion_callback=get_mi_token
)

# 3. Use the credential to access Blob Storage
blob_service_client = BlobServiceClient(
    account_url=f"https://{STORAGE_ACCOUNT}.blob.core.windows.net",
    credential=credential
)
container_client = blob_service_client.get_container_client(CONTAINER_NAME)

# 4. List blobs in the container
print(f"Blobs in container '{CONTAINER_NAME}':")
for blob in container_client.list_blobs():
    print(f"- {blob.name}")
```



### Java

For Java, Azure SDKs offer ManagedIdentityCredential and ClientAssertionCredential as well (in the Azure Identity library). The example below shows connecting to Azure Key Vault to retrieve a secret.

This example shows how to use a managed identity token as a federated credential to authenticate a Microsoft Entra app and list blobs from an Azure Storage container using the Azure Identity and Storage SDKs.

```java
import com.azure.identity.ManagedIdentityCredential;
import com.azure.identity.ManagedIdentityCredentialBuilder;
import com.azure.identity.ClientAssertionCredential;
import com.azure.identity.ClientAssertionCredentialBuilder;
import com.azure.core.credential.TokenRequestContext;
import com.azure.core.credential.TokenCredential;
import com.azure.storage.blob.BlobServiceClient;
import com.azure.storage.blob.BlobServiceClientBuilder;
import com.azure.storage.blob.models.BlobItem;
import com.azure.storage.blob.BlobContainerClient;

public class BlobStorageFederationExample 
{
    private static final String MI_CLIENT_ID       = "YOUR_MI_CLIENT_ID";
    private static final String APP_CLIENT_ID      = "YOUR_APP_CLIENT_ID";
    private static final String RESOURCE_TENANT_ID = "YOUR_RESOURCE_TENANT_ID";
    private static final String AUDIENCE           = "api://AzureADTokenExchange";
    private static final String STORAGE_ACCOUNT    = "YOUR_STORAGE_ACCOUNT_NAME";
    private static final String CONTAINER_NAME     = "YOUR_CONTAINER_NAME";

    public static void main(String[] args) {
        // 1. Build ManagedIdentityCredential to get tokens from the managed identity
        ManagedIdentityCredential miCredential = new ManagedIdentityCredentialBuilder()
                .clientId(MI_CLIENT_ID) // 
                .build();

        // 2. Build a ClientAssertionCredential using the MI token as the assertion
        ClientAssertionCredential clientAssertionCred = new ClientAssertionCredentialBuilder()
                .tenantId(RESOURCE_TENANT_ID)
                .clientId(APP_CLIENT_ID)
                .clientAssertion(() -> {
                    TokenRequestContext ctx = new TokenRequestContext().addScopes(AUDIENCE + "/.default");
                    return miCredential.getToken(ctx).block().getToken();
                })
                .build();

        // 3. Use the credential to access Blob Storage and list blobs
        BlobServiceClient blobServiceClient = new BlobServiceClientBuilder()
                .endpoint("https://" + STORAGE_ACCOUNT + ".blob.core.windows.net")
                .credential(clientAssertionCred)
                .buildClient();

        BlobContainerClient containerClient = blobServiceClient.getBlobContainerClient(CONTAINER_NAME);

        System.out.println("Blobs in container '" + CONTAINER_NAME + "':");
        for (BlobItem blob : containerClient.listBlobs()) {
            System.out.println("- " + blob.getName());
        }
    }
}
```


### Go

In Go, the Azure SDK (`azidentity` package) can be used to achieve a similar outcome. Below is an example using Azure Identity for Go:

This Go code sets up a `ManagedIdentityCredential` and a `ClientAssertionCredential`. The `GetToken` call at the end demonstrates obtaining an app token (in a real scenario, you would use the credApp directly when constructing a service client SDK, which would call GetToken internally). If you were accessing Key Vault, for example, you might use the Key Vault SDK providing credApp as the credential, similar to other languages.

> Note: The Go MSAL library does not yet natively support managed identities either. The below approach with azidentity is the recommended method.

```go
import (
    "context"
    "log"

    "github.com/Azure/azure-sdk-for-go/sdk/azcore/policy"
    "github.com/Azure/azure-sdk-for-go/sdk/azidentity"
)

func main() {
    appClientID           := "YOUR_APP_CLIENT_ID"
    tenantID              := "YOUR_RESOURCE_TENANT_ID"
    managedIdentityClientID := "YOUR_MANAGED_IDENTITY_CLIENT_ID"  // user-assigned MI client ID
    scopes               := []string{"api://AzureADTokenExchange/.default"}

    // 1. Create a ManagedIdentityCredential to get the MI token
    credMI, err := azidentity.NewManagedIdentityCredential(&azidentity.ManagedIdentityCredentialOptions{
        ID: azidentity.ClientID(managedIdentityClientID),
    })
    if err != nil {
        log.Fatalf("Failed to create ManagedIdentityCredential: %v", err)
    }

    // Define a function that will retrieve the MI token (for the assertion)
    getAssertion := func(ctx context.Context) (string, error) {
        tk, err := credMI.GetToken(ctx, policy.TokenRequestOptions{Scopes: scopes})
        if err != nil {
            return "", err
        }
        return tk.Token, nil
    }

    // 2. Create a ClientAssertionCredential for the Entra app, using the getAssertion callback
    credApp, err := azidentity.NewClientAssertionCredential(tenantID, appClientID, getAssertion, nil)
    if err != nil {
        log.Fatalf("Failed to create ClientAssertionCredential: %v", err)
    }

    // 3. Use credApp to get an access token for the target resource (example: Key Vault scope, Blob scope, etc.)
    // Here, we use the same "AzureADTokenExchange" scope just to illustrate obtaining a token; in practice use the resource's scope.
    token, err := credApp.GetToken(context.Background(), policy.TokenRequestOptions{Scopes: scopes})
    if err != nil {
        log.Fatalf("Failed to get token for app: %v", err)
    }

    // Use token.Token to call the target resource (e.g., include in Authorization header of an HTTP request,
    // or configure an Azure SDK client with a bearer token). For instance, you could create a Key Vault client with a TokenCredential that uses this token.
    log.Println("Got App token:", token.Token[:50], "...")
}
```



## Supported environments and scenarios

**Azure compute environments**: This federated identity pattern is supported on any Azure compute/service that can provide a managed identity. This includes Azure App Service, Azure Functions, Azure Container Instances, and Virtual Machines. One notable exception is Azure Kubernetes Service, which has its own workload identity integration. Keep in mind that only user-assigned identities are supported.

- **Same-tenant resource access**: A straight-forward use case for this capability is when your app and target resource both reside in the same tenant. It is recommended in this case to use a managed identity to directly access the target resource, but there are cases when an app is required, e.g. to use delegated permissions and act on-behalf-of the signed in user.


- **Cross-tenant resource access**: If you are targeting to access an resource (like Key Vault, Storage, SQL, Cosmos DB, Power BI, Microsoft Graph) that resides in a different Entra ID tenant than your application's home tenant, a managed identity by itself cannot directly get a token for that foreign tenant. By using a federated credential, the managed identity (in Tenant A) can exchange it's token for an application token that is recognized in Tenant B. This allows credential-less multi-tenant service-to-service access. For instance, Azure services enabling customer-managed keys (CMK) use this approach. For example, a service owned by a mmanaged service provider, can access a customer's Key Vault in the remote customer tenant by exchanging its managed identity token for a token in the customer tenant. (Azure Storage, Disks, SQL DB, Cosmos DB cross-tenant keys, all support this pattern under the hood.)

**Cloud considerations**: ensure you use the correct audience URL for your cloud. The default api://AzureADTokenExchange is for the global Azure cloud (Public). Government, China, etc., have their own as noted earlier​. This must match what you configured in the Federated credential on the app, otherwise the token exchange will fail validation. This feature does not work across cloud.

**Permissions recap**: The Entra application's service principal is what ultimately accesses the target resource. So all access checks are against that principal's permissions, either those directly assigned to it, or delegated by users. The managed identity itself does not need direct permission on the target resource (unless you are also using it directly elsewhere). The key is that the app gets the privileges. Ensure the app has the needed roles in whatever subscription or resource it's accessing. If troubleshooting, remember to check the sign-in logs in Entra ID for the application's sign-in attempt and the token issuance.
