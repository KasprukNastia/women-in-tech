namespace MiFicExamples.Vault.Configuration
{
    public record KeyVaultConfig
    {
        public required KeyVaultConfigParams Local { get; init; }

        public KeyVaultConfigParams? Remote { get; }

        public KeyVaultConfig(IConfiguration configuration)
        {
            var keyVaultConfigSection = configuration.GetSection(nameof(KeyVaultConfig));
            var localKeyVaultConfigSection = keyVaultConfigSection.GetSection(nameof(Local));
            Local = new KeyVaultConfigParams
            {
                Uri = localKeyVaultConfigSection[nameof(KeyVaultConfigParams.Uri)] ??
                    throw new ArgumentException("Local Key Vault Uri cannot be null"),
                SecretName = localKeyVaultConfigSection[nameof(KeyVaultConfigParams.SecretName)] ??
                    throw new ArgumentException("Local Key Vault secret name cannot be null")
            };
            var remoteKeyVaultConfigSection = keyVaultConfigSection.GetSection(nameof(Remote));
            Remote = new KeyVaultConfigParams
            {
                Uri = remoteKeyVaultConfigSection[nameof(KeyVaultConfigParams.Uri)],
                SecretName = remoteKeyVaultConfigSection[nameof(KeyVaultConfigParams.SecretName)]
            };
        }
    }
}
