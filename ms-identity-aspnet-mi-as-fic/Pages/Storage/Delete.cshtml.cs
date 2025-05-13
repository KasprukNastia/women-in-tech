using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MiFicExamples.Models;

namespace MiFicExamples.Pages.AzureStorage
{
    public class DeleteModel : PageModel
    {
        private readonly MiFicExamples.Data.CommentsContext _context;
        public DeleteModel(MiFicExamples.Data.CommentsContext context)
        {
            _context = context;
            Comment = new();
        }
        [BindProperty]
        public Comment Comment { get; set; }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            List<Comment> comments = await _context.GetComments();
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

            List<Comment> comments = await _context.GetComments();
            Comment = comments.FirstOrDefault(m => m.Name == id)!;

            if (Comment != null)
            {
                await _context.DeleteComment(Comment);
            }

            return RedirectToPage("./Index");
        }
    }
}
