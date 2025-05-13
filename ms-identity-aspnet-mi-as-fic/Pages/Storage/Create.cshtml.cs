using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MiFicExamples.Models;

namespace MiFicExamples.Pages.AzureStorage
{
    public class CreateModel : PageModel
    {
        private readonly MiFicExamples.Data.CommentsContext _context;
        public CreateModel(MiFicExamples.Data.CommentsContext context)
        {
            _context = context;
            Comment = new();
        }
        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public Comment Comment { get; set; }

        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            await _context.CreateComment(Comment);

            return RedirectToPage("./Index");
        }
    }
}
