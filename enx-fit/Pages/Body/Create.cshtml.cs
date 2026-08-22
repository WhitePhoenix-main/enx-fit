using enx_fit.Services;
using enx_fit.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Pages.Body;

public class CreateModel(BodyMeasurementService measurementService) : PageModel
{
    [BindProperty]
    public BodyMeasurementInputModel Input { get; set; } = new();

    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await measurementService.CreateAsync(Input);
        TempData["StatusMessage"] = "Body measurement added.";
        return RedirectToPage("./Index");
    }
}
