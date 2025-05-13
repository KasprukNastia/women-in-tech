using Azure.Identity;
using System.Text;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Collections.Generic;
using MiFicExamples.Models;

namespace MiFicExamples.Helpers
{
    public static class StorageHelper
    {
        static async Task<BlobContainerClient> GetBlobClientUsingMiFic(string managedIdentityClientId, string tenantId, string appClientId, string accountName, string containerName)
        {
            ArgumentException.ThrowIfNullOrEmpty(managedIdentityClientId);
            ArgumentException.ThrowIfNullOrEmpty(tenantId);
            ArgumentException.ThrowIfNullOrEmpty(appClientId);
            ArgumentException.ThrowIfNullOrEmpty(accountName);
            ArgumentException.ThrowIfNullOrEmpty(containerName);
            
            // Construct the blob container endpoint from the arguments.
            string containerEndpoint = string.Format("https://{0}.blob.core.windows.net/{1}",
                                                        accountName,
                                                        containerName);
            
            string audience = "api://AzureADTokenExchange";
            
            var miCredential = new ManagedIdentityCredential(managedIdentityClientId);

            ClientAssertionCredential assertion = new(
                tenantId,
                appClientId,
                async (token) =>
                {
                    // fetch Managed Identity token for the specified audience
                    var tokenRequestContext = new Azure.Core.TokenRequestContext(new[] { $"{audience}/.default" });
                    var accessToken = await miCredential.GetTokenAsync(tokenRequestContext).ConfigureAwait(false);
                    return accessToken.Token;
                });

            // Get a credential and create a client object for the blob container.
            var containerClient = new BlobContainerClient(new Uri(containerEndpoint), assertion);
            
            // Verify the connection by checking if the container exists
            await containerClient.ExistsAsync().ConfigureAwait(false);
            
            return containerClient;
        }

        private static async Task<BlobContainerClient> GetContainerClient(IConfiguration entraIdConfig, AzureStorageConfig azureStorageConfig)
        {
            ArgumentNullException.ThrowIfNull(entraIdConfig);
            ArgumentNullException.ThrowIfNull(azureStorageConfig);

            var managedIdentityClientId = entraIdConfig["AzureAd:ClientCredentials:0:ManagedIdentityClientId"] 
                ?? throw new InvalidOperationException("ManagedIdentityClientId configuration is missing");
            var tenantId = entraIdConfig["AzureAd:TenantId"] 
                ?? throw new InvalidOperationException("TenantId configuration is missing");
            var clientId = entraIdConfig["AzureAd:ClientId"] 
                ?? throw new InvalidOperationException("ClientId configuration is missing");
            
            ArgumentException.ThrowIfNullOrEmpty(azureStorageConfig.AccountName, nameof(azureStorageConfig.AccountName));
            ArgumentException.ThrowIfNullOrEmpty(azureStorageConfig.ContainerName, nameof(azureStorageConfig.ContainerName));

            return await GetBlobClientUsingMiFic(
                managedIdentityClientId,
                tenantId,
                clientId,
                azureStorageConfig.AccountName,
                azureStorageConfig.ContainerName
            ).ConfigureAwait(false);
        }

        static public async Task UploadBlob(AzureStorageConfig azureStorageConfig, IConfiguration entraIdConfig, string blobName, string blobContents)
        {
            var containerClient = await GetContainerClient(entraIdConfig, azureStorageConfig);
            
            // Create the container if it does not exist.
            await containerClient.CreateIfNotExistsAsync();

            // Upload text to a new block blob.
            byte[] byteArray = Encoding.ASCII.GetBytes(blobContents);

            using (MemoryStream stream = new MemoryStream(byteArray))
            {
                await containerClient.UploadBlobAsync(blobName, stream);
            }
        }

        static public async Task DeleteBlob(AzureStorageConfig azureStorageConfig, IConfiguration entraIdConfig, string blobName)
        {
            var containerClient = await GetContainerClient(entraIdConfig, azureStorageConfig);

            var blob = containerClient.GetBlobClient(blobName);
            await blob.DeleteIfExistsAsync();            
        }

        static public async Task<List<CommentBlobDTO>> GetBlobs(AzureStorageConfig azureStorageConfig, IConfiguration entraIdConfig)
        {
            
            List<CommentBlobDTO> blobs = new List<CommentBlobDTO>();
            var containerClient = await GetContainerClient(entraIdConfig, azureStorageConfig);

            await containerClient.CreateIfNotExistsAsync();

            await foreach (BlobItem blob in containerClient.GetBlobsAsync())
            {
                
                // Download the blob's contents and save it to a file
                // Get a reference to a blob named "sample-file"
                BlobClient blobClient = containerClient.GetBlobClient(blob.Name);
                BlobDownloadInfo download = await blobClient.DownloadAsync();

                byte[] bytes;
                using (MemoryStream stream = new MemoryStream())
                {
                    await download.Content.CopyToAsync(stream);
                    bytes = stream.ToArray();

                }

                String txt = new String(Encoding.ASCII.GetString(bytes));

                CommentBlobDTO blobDTO;
                blobDTO.Name = blob.Name;
                blobDTO.Contents = txt;
                blobs.Add(blobDTO);
            }

            return blobs;
        }
        
        static ILogger GetLogger(string name)
        {

            var factory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole(); // Logs to console
                builder.AddAzureWebAppDiagnostics();
            });

            return factory.CreateLogger(name);        
        }
    }
    

    public struct CommentBlobDTO
    {
        public string Name;
        public string Contents;

    }
}
