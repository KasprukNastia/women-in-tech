using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;

namespace MiFicExamples.Pages.Vault;

public class IndexModel : PageModel
{
    private readonly IConfiguration _configuration;
    
    public string MsiToken { get; private set; } = string.Empty;

    public string DefaultMsiToken { get; private set; } = string.Empty;

    public string SecretFromAnotherTenantUsingMsiFic { get; private set; } = string.Empty;


    public IndexModel(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task OnGetAsync()
    {
        // load the secrets..
        SecretFromAnotherTenantUsingMsiFic = await GetSecretFromAnotherTenantUsingMsiFic();
    }

    public async Task<string> GetSecretFromAnotherTenantUsingMsiFic()
    {        
        try
        {            
            var keyVaultTenantId = _configuration["KeyVault:TenantId"];
            var keyVaultUri = _configuration["KeyVault:Uri"];
            var secretName = _configuration["KeyVault:SecretName"];
            
            var clientId = _configuration["AzureAd:ClientId"];            
            var msiClientId = _configuration["AzureAd:ClientCredentials:0:ManagedIdentityClientId"];

            string audience = "api://AzureADTokenExchange";
            var miCredential = new ManagedIdentityCredential(msiClientId);

            ClientAssertionCredential assertion = new(                
                keyVaultTenantId, // note that this value must be the keyvault's tenant id
                clientId,
                async (token) => await GetManagedIdentityToken(miCredential, audience));
            
            if (string.IsNullOrEmpty(keyVaultUri))
            {
                throw new ArgumentNullException(nameof(keyVaultUri), "KeyVault URI cannot be null or empty.");
            }
            // Create a new SecretClient using the assertion
            var secretClient = new SecretClient(new Uri(keyVaultUri), assertion);

            // Retrieve the secret
            KeyVaultSecret secret = await secretClient.GetSecretAsync(secretName);

            return secret.Value;
        }
        catch (Exception ex)
        {
            return $"Error fetching secret from the other tenant: {ex.Message}, Full Trace: {ex.ToString()}";
        }
    }

    static async Task<string> GetManagedIdentityToken(ManagedIdentityCredential miCredential, string audience)
    {
        // we must request the managed identity token with the specified audience for federation to work.
        return (await miCredential.GetTokenAsync(new Azure.Core.TokenRequestContext([$"{audience}/.default"])).ConfigureAwait(false)).Token;
    }
}
