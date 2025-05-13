using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using Microsoft.Kiota.Abstractions.Authentication;

namespace MiFicExamples.Pages.Graph
{
    [AuthorizeForScopes(Scopes = new[] { "User.Read" })]
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IConfidentialClientApplication _app;
        private readonly IConfiguration _configuration;
        private readonly GraphServiceClient _graphServiceClient;

        public IndexModel(ILogger<IndexModel> logger, IConfiguration configuration, GraphServiceClient graphServiceClient)
        {
            _logger = logger;
            //_app = app;
            _configuration = configuration;
            _graphServiceClient = graphServiceClient;
        }

        public async Task OnGetAsync()
        {
            //var accessTokenProvider = new BaseBearerTokenAuthenticationProvider(new CustomAccessTokenProvider(_app));
            //var graphServiceClient = new GraphServiceClient(accessTokenProvider);

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


    public class CustomAccessTokenProvider : IAccessTokenProvider
    {
        private readonly IConfidentialClientApplication _confidentialClientApplication;

        public CustomAccessTokenProvider(IConfidentialClientApplication confidentialClientApplication)
        {
            _confidentialClientApplication = confidentialClientApplication;
        }

        public AllowedHostsValidator AllowedHostsValidator { get; } = new AllowedHostsValidator();

        public async Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
        {
            var scopes = new[] { "https://graph.microsoft.com/.default" };
            var result = await _confidentialClientApplication.AcquireTokenForClient(scopes).ExecuteAsync(cancellationToken);
            return result.AccessToken;
        }
    }

}

