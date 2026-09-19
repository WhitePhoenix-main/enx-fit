using enx_fit.Security;
using enx_fit.Models;
using enx_fit.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Pages.Exercises;

[MinimumRole(UserRole.Administrator)]
public class DeleteModel(ExerciseService exerciseService) : PageModel
{
    [BindProperty]
    public Exercise Exercise { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id) =>
        await LoadPageAsync(id);

    public async Task<IActionResult> OnPostAsync()
    {
        var result = await exerciseService.DeleteAsync(Exercise.Id);

        if (result == ExerciseDeleteResult.NotFound)
        {
            return NotFound();
        }

        if (result == ExerciseDeleteResult.InUse)
        {
            ModelState.AddModelError(string.Empty, "Упражнение используется в тренировках и не может быть удалено.");
            return await LoadPageAsync(Exercise.Id);
        }

        TempData["StatusMessage"] = "Упражнение удалено.";
        return RedirectToPage("./Index");
    }

    private async Task<IActionResult> LoadPageAsync(int id)
    {
        var exercise = await exerciseService.FindAsync(id);

        if (exercise is null)
        {
            return NotFound();
        }

        Exercise = exercise;
        return Page();
    }
}
