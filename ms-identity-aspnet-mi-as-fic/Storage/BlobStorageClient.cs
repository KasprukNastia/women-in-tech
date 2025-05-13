using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using MiFicExamples.Storage.Configuration;
using System.Text;

namespace MiFicExamples.Storage;

public class BlobStorageClient : IBlobStorageClient
{
    private readonly BlobClientConfig _blobClientConfig;

    public BlobStorageClient(BlobClientConfig blobClientConfig)
    {
        _blobClientConfig = blobClientConfig;
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
            await containerClient.UploadBlobAsync(blob.Name, contentStream);
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

            blobs.Add(new Blob { Name = blob.Name, Content = blobContent });
        }

        return blobs;
    }

    private async Task<BlobContainerClient> CreateBlobStorageClient(BlobStorageConfig blobStorageConfig)
    {
        var miCredential = new ManagedIdentityCredential(_blobClientConfig.ManagedIdentityClientId);

        ClientAssertionCredential creds = new(
            _blobClientConfig.TenantId,
            _blobClientConfig.AppClientId,
            async (token) =>
            {
                // fetch Managed Identity token for the specified audience
                var tokenRequestContext = new Azure.Core.TokenRequestContext([$"{_blobClientConfig.AuthTokenAudience}/.default"]);
                var accessToken = await miCredential.GetTokenAsync(tokenRequestContext).ConfigureAwait(false);
                return accessToken.Token;
            });

        // Get a credential and create a client object for the blob container.
        var containerClient = new BlobContainerClient(new Uri(blobStorageConfig.ContainerEndpoint), creds);

        // Verify the connection by checking if the container exists
        await containerClient.ExistsAsync().ConfigureAwait(false);

        return containerClient;
    }
}
