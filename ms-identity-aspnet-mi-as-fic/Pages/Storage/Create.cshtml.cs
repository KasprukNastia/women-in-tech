using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MiFicExamples.Auth.Configuration;
using MiFicExamples.Storage;
using MiFicExamples.Storage.Configuration;

namespace MiFicExamples.Pages.Storage
{
    public class CreateModel : PageModel
    {
        private readonly IBlobStorageClient _blobStorageClient;
        private readonly AzureStorageConfig _blobStorageConfig;

        [BindProperty]
        public Blob Blob { get; set; }

        public bool UseManagedIdentity { get; set; }

        public CreateModel(IBlobStorageClient blobStorageClient,
            AzureStorageConfig blobStorageConfig,
            AuthConfig authConfig)
        {
            _blobStorageClient = blobStorageClient;
            _blobStorageConfig = blobStorageConfig;
            Blob = new();
            UseManagedIdentity = authConfig.UseManagedIdentity;
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
