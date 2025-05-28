using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Graph;
using Microsoft.Identity.Web;

namespace MiFicExamples.Pages.Graph
{
    [AuthorizeForScopes(Scopes = new[] { "User.Read" })]
    public class IndexModel : PageModel
    {
        private readonly GraphServiceClient _graphServiceClient;

        public UserDto CurrentUser { get; set; }

        public IndexModel(GraphServiceClient graphServiceClient)
        {
            _graphServiceClient = graphServiceClient;
            CurrentUser = new UserDto();
        }

        public async Task OnGetAsync()
        {
            var user = await _graphServiceClient.Me.GetAsync();

            CurrentUser = new UserDto
            {
                Name = user!.DisplayName ?? string.Empty,
                CompanyName = user.CompanyName,
                JobTitle = user.JobTitle,
                City = user.City,
            };
        }
    }

    public record UserDto
    {
        public string Name { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string? JobTitle { get; set; }
        public string? City { get; set; }
    }
}

