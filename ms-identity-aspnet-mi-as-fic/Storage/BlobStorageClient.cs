using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using MiFicExamples.Auth;
using MiFicExamples.Auth.Configuration;
using MiFicExamples.Storage.Configuration;
using System.Text;

namespace MiFicExamples.Storage;

public class BlobStorageClient : IBlobStorageClient
{
    private readonly AuthConfig _authConfig;
    private readonly IClientAssertionCredentialFactory _clientAssertionCredentialFactory;

    public BlobStorageClient(AuthConfig authConfig,
        IClientAssertionCredentialFactory clientAssertionCredentialFactory)
    {
        _authConfig = authConfig;
        _clientAssertionCredentialFactory = clientAssertionCredentialFactory;
    }

    public async Task UploadBlobToStorage(BlobStorageConfig blobStorageConfig, Blob blob)
    {
        var containerClient = await CreateBlobStorageClient(blobStorageConfig);

        // Create the container if it does not exist.
        await containerClient.CreateIfNotExistsAsync();

        // Upload text to a new block blob.
        byte[] byteArray = Encoding.ASCII.GetBytes(blob.Content);

        using (MemoryStream contentStream = new MemoryStream(byteArray))
        {
            await containerClient.UploadBlobAsync(blob.Key, contentStream);
        }
    }

    public async Task DeleteBlobFromStorage(BlobStorageConfig blobStorageConfig, string blobName)
    {
        var containerClient = await CreateBlobStorageClient(blobStorageConfig);

        var blob = containerClient.GetBlobClient(blobName);
        await blob.DeleteIfExistsAsync();
    }

    public async Task<List<Blob>> GetAllBlobsFromStorage(BlobStorageConfig blobStorageConfig)
    {
        var containerClient = await CreateBlobStorageClient(blobStorageConfig);
        await containerClient.CreateIfNotExistsAsync();

        var blobs = new List<Blob>();
        await foreach (BlobItem blob in containerClient.GetBlobsAsync())
        {
            BlobClient blobClient = containerClient.GetBlobClient(blob.Name);
            BlobDownloadInfo download = await blobClient.DownloadAsync();

            byte[] bytes;
            using (MemoryStream stream = new MemoryStream())
            {
                await download.Content.CopyToAsync(stream);
                bytes = stream.ToArray();
            }

            var blobContent = new string(Encoding.ASCII.GetString(bytes));

            blobs.Add(new Blob { Key = blob.Name, Content = blobContent });
        }

        return blobs;
    }

    private async Task<BlobContainerClient> CreateBlobStorageClient(BlobStorageConfig blobStorageConfig)
    {
        var creds = _clientAssertionCredentialFactory.GetClientAssertionCredential(
            _authConfig.TenantId,
            _authConfig.AppClientId,
            _authConfig.ManagedIdentityClientId,
            [$"{_authConfig.AuthTokenAudience}/.default"]);

        // Get a credential and create a client object for the blob container.
        var containerClient = new BlobContainerClient(new Uri(blobStorageConfig.ContainerEndpoint), creds);

        // Verify the connection by checking if the container exists
        await containerClient.ExistsAsync().ConfigureAwait(false);

        return containerClient;
    }
}
