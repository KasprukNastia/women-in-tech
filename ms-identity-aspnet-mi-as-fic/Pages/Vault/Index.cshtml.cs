using Azure.Core;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MiFicExamples.Auth;
using MiFicExamples.Auth.Configuration;
using MiFicExamples.Vault.Configuration;

namespace MiFicExamples.Pages.Vault;

public class IndexModel : PageModel
{
    private readonly ICredentialFactory _credentialsFactory;
    private readonly AuthConfig _authConfig;
    private readonly KeyVaultConfig _keyVaultConfig;

    public string LocalSecretName { get; private set; } = string.Empty;
    public string LocalSecretValue { get; private set; } = string.Empty;

    public string? RemoteSecretName { get; private set; } = string.Empty;
    public string? RemoteSecretValue { get; private set; } = string.Empty;

    public bool UseManagedIdentity { get; set; }

    public IndexModel(ICredentialFactory credentialsFactory,
        AuthConfig authConfig,
        KeyVaultConfig keyVaultConfig)
    {
        _credentialsFactory = credentialsFactory;
        _authConfig = authConfig;
        _keyVaultConfig = keyVaultConfig;
        UseManagedIdentity = authConfig.UseManagedIdentity;
    }

    public async Task OnGetAsync()
    {
        LocalSecretName = _keyVaultConfig.Local.SecretName!;
        LocalSecretValue = await GetSecret(_keyVaultConfig.Local);

        if (!string.IsNullOrWhiteSpace(_keyVaultConfig?.Remote?.Uri) && !string.IsNullOrWhiteSpace(_keyVaultConfig.Remote.SecretName))
        {
            RemoteSecretName = _keyVaultConfig.Remote.SecretName;
            RemoteSecretValue = await GetSecret(_keyVaultConfig.Remote);
        }
    }

    public async Task<string> GetSecret(KeyVaultConfigParams keyVaultConfigParams)
    {
        try
        {
            TokenCredential creds;
            if (!_authConfig.UseManagedIdentity)
            {
                creds = _credentialsFactory.GetClientSecretCredentials(
                    _authConfig.TenantId,
                    _authConfig.AppClientId,
                    _authConfig.ClientSecret);
            }
            else
            {
                creds = _credentialsFactory.GetManagedIdentityCredentials(
                    _authConfig.TenantId,
                    _authConfig.AppClientId,
                    _authConfig.ManagedIdentityClientId);
            }

            // Create a new SecretClient using creds
            var secretClient = new SecretClient(new Uri(keyVaultConfigParams.Uri!), creds);

            // Retrieve the secret
            KeyVaultSecret secret = await secretClient.GetSecretAsync(keyVaultConfigParams.SecretName);

            return secret.Value;
        }
        catch (Exception ex)
        {
            return $"Error fetching secret from the other tenant: {ex.Message}. Full Trace: {ex.ToString()}";
        }
    }
}
