using enx_fit.Services;
using enx_fit.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Pages.Exercises;

public class CreateModel(ExerciseService exerciseService) : PageModel
{
    [BindProperty]
    public ExerciseInputModel Input { get; set; } = new();

    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await exerciseService.CreateAsync(Input);

        if (result == ExerciseWriteResult.DuplicateName)
        {
            ModelState.AddModelError("Input.Name", "An exercise with this name already exists.");
            return Page();
        }

        TempData["StatusMessage"] = "Exercise created.";
        return RedirectToPage("./Index");
    }
}
