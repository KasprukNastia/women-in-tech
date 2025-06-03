using Microsoft.AspNetCore.Mvc.RazorPages;
using MiFicExamples.Auth.Configuration;
using MiFicExamples.Storage;
using MiFicExamples.Storage.Configuration;

namespace MiFicExamples.Pages.Storage
{
    public class IndexModel : PageModel
    {
        private readonly IBlobStorageClient _blobStorageClient;
        private readonly AzureStorageConfig _blobStorageConfig;

        public IList<Blob> Blobs { get; set; }
        public bool UseManagedIdentity { get; set; }

        public IndexModel(IBlobStorageClient blobStorageClient,
            AzureStorageConfig blobStorageConfig,
            AuthConfig authConfig)
        {
            _blobStorageClient = blobStorageClient;
            _blobStorageConfig = blobStorageConfig;
            Blobs = new List<Blob>();
            UseManagedIdentity = authConfig.UseManagedIdentity;
        }

        public async Task OnGetAsync()
        {
            Blobs = await _blobStorageClient.GetAllBlobsFromStorage(_blobStorageConfig);
        }
    }
}
