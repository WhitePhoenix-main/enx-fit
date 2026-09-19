using enx_fit.Security;
using enx_fit.Services;
using enx_fit.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Pages.Exercises;

[MinimumRole(UserRole.Administrator)]
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
            ModelState.AddModelError("Input.Name", "Упражнение с таким названием уже существует.");
            return Page();
        }

        TempData["StatusMessage"] = "Упражнение создано.";
        return RedirectToPage("./Index");
    }
}
