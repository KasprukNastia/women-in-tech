using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MiFicExamples.Comments;

namespace MiFicExamples.Pages.Storage
{
    public class DeleteModel : PageModel
    {
        private readonly ICommentsProvider _commentsProvider;

        [BindProperty]
        public Comment Comment { get; set; }

        public DeleteModel(ICommentsProvider commentsProvider)
        {
            _commentsProvider = commentsProvider;
            Comment = new();
        }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            List<Comment> comments = await _commentsProvider.GetAllComments();
            Comment = comments.FirstOrDefault(m => m.Name == id) ?? throw new InvalidOperationException("Comment not found.");

            if (Comment == null)
            {
                return NotFound();
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            List<Comment> comments = await _commentsProvider.GetAllComments();
            Comment = comments.FirstOrDefault(m => m.Name == id)!;

            if (Comment != null)
            {
                await _commentsProvider.DeleteComment(Comment);
            }

            return RedirectToPage("./Index");
        }
    }
}
