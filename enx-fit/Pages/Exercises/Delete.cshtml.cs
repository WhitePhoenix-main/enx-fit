using enx_fit.Models;
using enx_fit.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Pages.Exercises;

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
            ModelState.AddModelError(string.Empty, "This exercise is used by a workout and cannot be deleted.");
            return await LoadPageAsync(Exercise.Id);
        }

        TempData["StatusMessage"] = "Exercise deleted.";
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
