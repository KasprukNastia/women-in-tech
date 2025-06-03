namespace MiFicExamples.Auth.Configuration;

public record AuthConfig
{
    public required string TenantId { get; init; }
    public required string AppClientId { get; init; }
    public required string ManagedIdentityClientId { get; init; }
    public required string ClientSecret { get; init; }
    public required bool UseManagedIdentity { get; init; }

    public AuthConfig(IConfiguration configuration)
    {
        var azureAdSection = configuration.GetSection(nameof(AuthConfig));

        TenantId = azureAdSection[nameof(TenantId)] ?? throw new ArgumentException($"{nameof(TenantId)} cannot be null");
        AppClientId = azureAdSection[nameof(AppClientId)] ?? throw new ArgumentException($"{nameof(AppClientId)} cannot be null");
        ManagedIdentityClientId = azureAdSection[nameof(ManagedIdentityClientId)]!;
        ClientSecret = azureAdSection[nameof(ClientSecret)]!;
        UseManagedIdentity = bool.Parse(azureAdSection[nameof(UseManagedIdentity)]!);
    }
}
