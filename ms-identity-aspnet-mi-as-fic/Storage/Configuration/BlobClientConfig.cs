namespace MiFicExamples.Storage.Configuration;

public record BlobClientConfig
{
    public required string TenantId { get; init; }
    public required string AppClientId { get; init; }
    public required string ManagedIdentityClientId { get; init; }
    public required string AuthTokenAudience { get; init; }

    public BlobClientConfig(IConfiguration configuration)
    {
        var azureAdSection = configuration.GetSection("AzureAd");

        TenantId = azureAdSection[nameof(TenantId)]!;
        AppClientId = azureAdSection["ClientId"]!;
        ManagedIdentityClientId = azureAdSection[$"ClientCredentials:{nameof(ManagedIdentityClientId)}"]!;
        AuthTokenAudience = azureAdSection["ClientCredentials:TokenExchangeUrl"]!;
    }
}
