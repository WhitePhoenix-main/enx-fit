using enx_fit.Services;
using enx_fit.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Pages.Workouts;

public class EditModel(WorkoutService workoutService) : PageModel
{
    [BindProperty]
    public int Id { get; set; }

    [BindProperty]
    public WorkoutSessionInputModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var workout = await workoutService.FindAsync(id);

        if (workout is null)
        {
            return NotFound();
        }

        Id = workout.Id;
        Input = WorkoutSessionInputModel.FromWorkout(workout);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (!await workoutService.UpdateAsync(Id, Input))
        {
            return NotFound();
        }

        TempData["StatusMessage"] = "Workout updated.";
        return RedirectToPage("./Details", new { id = Id });
    }
}
