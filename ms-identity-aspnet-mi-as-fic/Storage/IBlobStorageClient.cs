using MiFicExamples.Storage.Configuration;

namespace MiFicExamples.Storage;

public interface IBlobStorageClient
{
    Task UploadBlobToStorage(BlobStorageConfig blobStorageConfig, Blob blob);
    Task DeleteBlobFromStorage(BlobStorageConfig blobStorageConfig, string blobName);
    Task<List<Blob>> GetAllBlobsFromStorage(BlobStorageConfig blobStorageConfig);
}
