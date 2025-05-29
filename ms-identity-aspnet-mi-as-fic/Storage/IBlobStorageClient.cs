using MiFicExamples.Storage.Configuration;

namespace MiFicExamples.Storage;

public interface IBlobStorageClient
{
    Task UploadBlobToStorage(AzureStorageConfig blobStorageConfig, Blob blob);
    Task DeleteBlobFromStorage(AzureStorageConfig blobStorageConfig, string blobName);
    Task<List<Blob>> GetAllBlobsFromStorage(AzureStorageConfig blobStorageConfig);
}
