using enx_fit.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Pages;

public sealed class FaqModel : PageModel
{
    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    public IReadOnlyList<PublicFaq> Questions { get; private set; } = [];
    public void OnGet()
    {
        Search = Search?.Trim();
        if (Search?.Length > 100) Search = Search[..100];
        Questions = PublicFaq.Find(Search);
        ModelState.Clear();
    }
}
