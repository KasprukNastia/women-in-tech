using Microsoft.AspNetCore.Mvc.RazorPages;
using MiFicExamples.Storage;
using MiFicExamples.Storage.Configuration;

namespace MiFicExamples.Pages.Storage
{
    public class IndexModel : PageModel
    {
        private readonly IBlobStorageClient _blobStorageClient;
        private readonly BlobStorageConfig _blobStorageConfig;

        public IList<Blob> Blobs { get; set; }

        public IndexModel(IBlobStorageClient blobStorageClient,
            BlobStorageConfig blobStorageConfig)
        {
            _blobStorageClient = blobStorageClient;
            _blobStorageConfig = blobStorageConfig;
            Blobs = new List<Blob>();
        }

        public async Task OnGetAsync()
        {
            Blobs = await _blobStorageClient.GetAllBlobsFromStorage(_blobStorageConfig);
        }
    }
}
