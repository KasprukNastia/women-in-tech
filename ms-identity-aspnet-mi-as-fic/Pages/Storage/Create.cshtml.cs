using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MiFicExamples.Storage;
using MiFicExamples.Storage.Configuration;

namespace MiFicExamples.Pages.Storage
{
    public class CreateModel : PageModel
    {
        private readonly IBlobStorageClient _blobStorageClient;
        private readonly BlobStorageConfig _blobStorageConfig;

        [BindProperty]
        public Blob Blob { get; set; }

        public CreateModel(IBlobStorageClient blobStorageClient,
            BlobStorageConfig blobStorageConfig)
        {
            _blobStorageClient = blobStorageClient;
            _blobStorageConfig = blobStorageConfig;
            Blob = new();
        }
        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            await _blobStorageClient.UploadBlobToStorage(_blobStorageConfig, Blob);

            return RedirectToPage("./Index");
        }
    }
}
