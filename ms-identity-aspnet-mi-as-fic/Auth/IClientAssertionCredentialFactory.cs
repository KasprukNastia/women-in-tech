using Azure.Identity;

namespace MiFicExamples.Auth
{
    public interface IClientAssertionCredentialFactory
    {
        ClientAssertionCredential GetClientAssertionCredential(
            string tenantId,
            string appClientId,
            string managedIdentityClientId,
            string[] authTokenScopes);
    }
}
