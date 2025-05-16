namespace MiFicExamples.Storage.Configuration;

public record BlobStorageConfig
{
    public required string AccountName { get; init; }
    public required string ContainerName { get; init; }
    public string ContainerEndpoint => $"https://{AccountName}.blob.core.windows.net/{ContainerName}";

    public BlobStorageConfig(IConfiguration configuration)
    {
        var azureStorageConfigSection = configuration.GetSection("AzureStorageConfig");

        AccountName = azureStorageConfigSection[nameof(AccountName)] ??
            throw new ArgumentException("Blob storage account name cannot be null");
        ContainerName = azureStorageConfigSection[nameof(ContainerName)] ??
            throw new ArgumentException("Blob storage container name cannot be null");
    }
}
