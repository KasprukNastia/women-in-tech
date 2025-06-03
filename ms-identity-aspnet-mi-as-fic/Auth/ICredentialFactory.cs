using Azure.Identity;

namespace MiFicExamples.Auth;

public interface ICredentialFactory
{
    ClientAssertionCredential GetManagedIdentityCredentials(
        string tenantId,
        string appClientId,
        string managedIdentityClientId,
        string[]? authTokenScopes = null);

    ClientSecretCredential GetClientSecretCredentials(
        string tenantId,
        string appClientId,
        string clientSecret);
}
