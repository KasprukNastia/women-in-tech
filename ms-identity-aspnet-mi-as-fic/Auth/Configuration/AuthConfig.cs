namespace MiFicExamples.Auth.Configuration;

public record AuthConfig
{
    public required string TenantId { get; init; }
    public required string AppClientId { get; init; }
    public required string ManagedIdentityClientId { get; init; }
    public required string AuthTokenAudience { get; init; }

    public AuthConfig(IConfiguration configuration)
    {
        var azureAdSection = configuration.GetSection("AzureAd");

        TenantId = azureAdSection[nameof(TenantId)] ?? throw new ArgumentException($"{nameof(TenantId)} cannot be null");
        AppClientId = azureAdSection["ClientId"] ?? throw new ArgumentException($"{nameof(AppClientId)} cannot be null");
        ManagedIdentityClientId = azureAdSection[$"ClientCredentials:0:{nameof(ManagedIdentityClientId)}"]
            ?? throw new ArgumentException($"{nameof(ManagedIdentityClientId)} cannot be null");
        AuthTokenAudience = azureAdSection["ClientCredentials:0:TokenExchangeUrl"]
            ?? throw new ArgumentException($"{nameof(AuthTokenAudience)} cannot be null");
    }
}
