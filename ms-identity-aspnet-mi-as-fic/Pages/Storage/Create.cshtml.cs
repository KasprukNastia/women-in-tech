using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MiFicExamples.Comments;

namespace MiFicExamples.Pages.Storage
{
    public class CreateModel : PageModel
    {
        private readonly ICommentsProvider _commentsProvider;

        [BindProperty]
        public Comment Comment { get; set; }

        public CreateModel(ICommentsProvider commentsProvider)
        {
            _commentsProvider = commentsProvider;
            Comment = new();
        }
        public IActionResult OnGet()
        {
            return Page();
        }

        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            await _commentsProvider.CreateComment(Comment);

            return RedirectToPage("./Index");
        }
    }
}
