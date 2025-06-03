namespace MiFicExamples.Vault.Configuration;

public record KeyVaultConfigParams
{
    public string? Uri { get; init; }
    public string? SecretName { get; init; }
}
