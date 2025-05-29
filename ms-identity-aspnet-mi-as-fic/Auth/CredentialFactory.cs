using Azure.Identity;

namespace MiFicExamples.Auth;

public class CredentialFactory : ICredentialFactory
{
    public ClientAssertionCredential GetManagedIdentityCredentials(
        string tenantId,
        string appClientId,
        string managedIdentityClientId,
        string[]? authTokenScopes = null)
    {
        authTokenScopes = authTokenScopes ?? [$"api://AzureADTokenExchange/.default"];

        var miCredential = new ManagedIdentityCredential(managedIdentityClientId);

        var options = new ClientAssertionCredentialOptions();
        options.AdditionallyAllowedTenants.Add("*");
        return new ClientAssertionCredential(
            tenantId,
            appClientId,
            async (token) =>
            {
                // fetch Managed Identity token for the specified audience
                var tokenRequestContext = new Azure.Core.TokenRequestContext(authTokenScopes);
                var accessToken = await miCredential.GetTokenAsync(tokenRequestContext).ConfigureAwait(false);
                return accessToken.Token;
            },
            options);
    }

    public ClientSecretCredential GetClientSecretCredentials(
        string tenantId,
        string appClientId,
        string clientSecret)
    {
        return new ClientSecretCredential(tenantId, appClientId, clientSecret);
    }
}
