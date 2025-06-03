using Azure.Core;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Graph;
using MiFicExamples.Auth;
using MiFicExamples.Auth.Configuration;

namespace MiFicExamples.Pages.Graph
{
    public class IndexModel : PageModel
    {
        private readonly ICredentialFactory _credentialsFactory;
        private readonly AuthConfig _authConfig;

        public IReadOnlyList<UserDto> Users { get; set; } = new List<UserDto>();
        public bool UseManagedIdentity { get; set; }

        public IndexModel(ICredentialFactory credentialsFactory, AuthConfig authConfig)
        {
            _credentialsFactory = credentialsFactory;
            _authConfig = authConfig;
            UseManagedIdentity = _authConfig.UseManagedIdentity;
        }

        public async Task OnGetAsync()
        {
            TokenCredential creds;
            if (!_authConfig.UseManagedIdentity)
            {
                creds = _credentialsFactory.GetClientSecretCredentials(
                    _authConfig.TenantId,
                    _authConfig.AppClientId,
                    _authConfig.ClientSecret);
            }
            else
            {
                creds = _credentialsFactory.GetManagedIdentityCredentials(
                    _authConfig.TenantId,
                    _authConfig.AppClientId,
                    _authConfig.ManagedIdentityClientId);
            }

            var graphServiceClient = new GraphServiceClient(creds);

            var users = await graphServiceClient.Users.GetAsync();

            if (users?.Value != null && users.Value.Any())
            {
                Users = users.Value.Select(u => new UserDto { DisplayName = u.DisplayName ?? string.Empty }).ToList();
            }
        }
    }

    public record UserDto
    {
        public string DisplayName { get; set; } = string.Empty;
    }
}

