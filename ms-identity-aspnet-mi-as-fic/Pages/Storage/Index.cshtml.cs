using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MiFicExamples.Models;
using System.Collections.Generic;

namespace MiFicExamples.Pages.AzureStorage
{
    public class IndexModel : PageModel
    {
        private readonly MiFicExamples.Data.CommentsContext _context;
        public IndexModel(MiFicExamples.Data.CommentsContext context)
        {
            _context = context;
            Comments = new List<Comment>();
        }

        public IList<Comment> Comments { get; set; }
        public async Task OnGetAsync()
        {
            Comments = await _context.GetComments();
        }
    }
}
