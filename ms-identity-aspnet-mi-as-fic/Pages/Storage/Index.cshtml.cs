using Microsoft.AspNetCore.Mvc.RazorPages;
using MiFicExamples.Comments;

namespace MiFicExamples.Pages.Storage
{
    public class IndexModel : PageModel
    {
        private readonly ICommentsProvider _commentsProvider;

        public IList<Comment> Comments { get; set; }

        public IndexModel(ICommentsProvider commentsProvider)
        {
            _commentsProvider = commentsProvider;
            Comments = new List<Comment>();
        }

        public async Task OnGetAsync()
        {
            Comments = await _commentsProvider.GetAllComments();
        }
    }
}
