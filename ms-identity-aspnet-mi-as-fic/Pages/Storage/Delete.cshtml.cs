using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MiFicExamples.Storage;
using MiFicExamples.Storage.Configuration;

namespace MiFicExamples.Pages.Storage
{
    public class DeleteModel : PageModel
    {
        private readonly IBlobStorageClient _blobStorageClient;
        private readonly BlobStorageConfig _blobStorageConfig;

        [BindProperty]
        public Blob Blob { get; set; }

        public DeleteModel(IBlobStorageClient blobStorageClient,
            BlobStorageConfig blobStorageConfig)
        {
            _blobStorageClient = blobStorageClient;
            _blobStorageConfig = blobStorageConfig;
            Blob = new();
        }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            List<Blob> Blobs = await _blobStorageClient.GetAllBlobsFromStorage(_blobStorageConfig);
            Blob = Blobs.FirstOrDefault(m => m.Key == id) ?? throw new InvalidOperationException("Blob not found.");

            if (Blob == null)
            {
                return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            List<Blob> Blobs = await _blobStorageClient.GetAllBlobsFromStorage(_blobStorageConfig);
            Blob = Blobs.FirstOrDefault(m => m.Key == id)!;

            if (Blob?.Key != null)
            {
                await _blobStorageClient.DeleteBlobFromStorage(_blobStorageConfig, Blob.Key);
            }

            return RedirectToPage("./Index");
        }
    }
}
