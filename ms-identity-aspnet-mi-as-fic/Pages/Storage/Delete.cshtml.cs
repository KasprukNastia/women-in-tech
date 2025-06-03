using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MiFicExamples.Auth.Configuration;
using MiFicExamples.Storage;
using MiFicExamples.Storage.Configuration;

namespace MiFicExamples.Pages.Storage
{
    public class DeleteModel : PageModel
    {
        private readonly IBlobStorageClient _blobStorageClient;
        private readonly AzureStorageConfig _blobStorageConfig;

        [BindProperty]
        public Blob Blob { get; set; }
        public bool UseManagedIdentity { get; set; }
        public DeleteModel(IBlobStorageClient blobStorageClient,
            AzureStorageConfig blobStorageConfig,
            AuthConfig authConfig)
        {
            _blobStorageClient = blobStorageClient;
            _blobStorageConfig = blobStorageConfig;
            Blob = new();
            UseManagedIdentity = authConfig.UseManagedIdentity;
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
