namespace MiFicExamples.Vault.Configuration
{
    public record KeyVaultConfig
    {
        public required string Uri { get; init; }
        public required string SecretName { get; init; }

        public KeyVaultConfig(IConfiguration configuration)
        {
            var keyVaultConfigSection = configuration.GetSection("KeyVault");

            Uri = keyVaultConfigSection[nameof(Uri)] ??
                throw new ArgumentException("Key vault Uri cannot be null");
            SecretName = keyVaultConfigSection[nameof(SecretName)] ??
                throw new ArgumentException("Key vault secret name cannot be null");
        }
    }
}
