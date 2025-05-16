using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MiFicExamples.Auth;
using MiFicExamples.Auth.Configuration;
using MiFicExamples.Vault.Configuration;

namespace MiFicExamples.Pages.Vault;

public class IndexModel : PageModel
{
    private readonly IClientAssertionCredentialFactory _clientAssertionCredentialFactory;
    private readonly AuthConfig _authConfig;
    private readonly KeyVaultConfig _keyVaultConfig;

    public string SecretValue { get; private set; } = string.Empty;

    public IndexModel(IClientAssertionCredentialFactory clientAssertionCredentialFactory,
        AuthConfig authConfig,
        KeyVaultConfig keyVaultConfig)
    {
        _clientAssertionCredentialFactory = clientAssertionCredentialFactory;
        _authConfig = authConfig;
        _keyVaultConfig = keyVaultConfig;
    }

    public async Task OnGetAsync()
    {
        // load the secrets..
        SecretValue = await GetSecretFromAnotherTenantUsingMsiFic();
    }

    public async Task<string> GetSecretFromAnotherTenantUsingMsiFic()
    {
        try
        {
            var creds = _clientAssertionCredentialFactory.GetClientAssertionCredential(
                _authConfig.TenantId,
                _authConfig.AppClientId,
                _authConfig.ManagedIdentityClientId,
                [$"{_authConfig.AuthTokenAudience}/.default"]);

            // Create a new SecretClient using creds
            var secretClient = new SecretClient(new Uri(_keyVaultConfig.Uri), creds);

            // Retrieve the secret
            KeyVaultSecret secret = await secretClient.GetSecretAsync(_keyVaultConfig.SecretName);

            return secret.Value;
        }
        catch (Exception ex)
        {
            return $"Error fetching secret from the other tenant: {ex.Message}. Full Trace: {ex.ToString()}";
        }
    }
}
