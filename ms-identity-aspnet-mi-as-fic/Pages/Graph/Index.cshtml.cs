using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Graph;
using Microsoft.Identity.Web;

namespace MiFicExamples.Pages.Graph
{
    [AuthorizeForScopes(Scopes = new[] { "User.Read" })]
    public class IndexModel : PageModel
    {
        private readonly GraphServiceClient _graphServiceClient;

        public IndexModel(GraphServiceClient graphServiceClient)
        {
            _graphServiceClient = graphServiceClient;
        }

        public async Task OnGetAsync()
        {
            var user = await _graphServiceClient.Me.GetAsync();
            ViewData["Me"] = user;
            ViewData["name"] = user?.DisplayName;

            using (var photoStream = await _graphServiceClient.Me.Photo.Content.GetAsync())
            {
                if (photoStream != null)
                {
                    MemoryStream ms = new MemoryStream();
                    photoStream.CopyTo(ms);
                    byte[] buffer = ms.ToArray();
                    ViewData["photo"] = Convert.ToBase64String(buffer);

                }
                else
                {
                    ViewData["photo"] = null;
                }
            }
        }
    }
}

