using enx_fit.Models;
using enx_fit.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Pages.Workouts;

public class DeleteModel(WorkoutService workoutService) : PageModel
{
    [BindProperty]
    public WorkoutSession Workout { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id) =>
        await LoadPageAsync(id);

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await workoutService.DeleteAsync(Workout.Id))
        {
            return NotFound();
        }

        TempData["StatusMessage"] = "Workout deleted.";
        return RedirectToPage("./Index");
    }

    private async Task<IActionResult> LoadPageAsync(int id)
    {
        var workout = await workoutService.FindAsync(id);

        if (workout is null)
        {
            return NotFound();
        }

        Workout = workout;
        return Page();
    }
}
