using enx_fit.Models;
using enx_fit.Services;
using enx_fit.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Pages.Workouts;

public class DetailsModel(WorkoutService workoutService) : PageModel
{
    public WorkoutSession Workout { get; private set; } = null!;

    public IReadOnlyList<Exercise> AvailableExercises { get; private set; } = [];

    [BindProperty]
    public AddWorkoutExerciseInputModel AddExercise { get; set; } = new();

    [BindProperty]
    public AddSetEntryInputModel AddSet { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id) =>
        await LoadPageAsync(id);

    public async Task<IActionResult> OnPostAddExerciseAsync(int id)
    {
        ModelState.Clear();

        if (!TryValidateModel(AddExercise, nameof(AddExercise)))
        {
            return await LoadPageAsync(id);
        }

        var result = await workoutService.AddExerciseAsync(id, AddExercise.ExerciseId);

        if (result == AddWorkoutExerciseResult.WorkoutNotFound)
        {
            return NotFound();
        }

        if (result == AddWorkoutExerciseResult.ExerciseNotFound)
        {
            ModelState.AddModelError("AddExercise.ExerciseId", "Select an existing exercise.");
            return await LoadPageAsync(id);
        }

        if (result == AddWorkoutExerciseResult.AlreadyAdded)
        {
            ModelState.AddModelError("AddExercise.ExerciseId", "This exercise is already in the workout.");
            return await LoadPageAsync(id);
        }

        TempData["StatusMessage"] = "Exercise added to workout.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAddSetAsync(int id)
    {
        ModelState.Clear();

        if (!TryValidateModel(AddSet, nameof(AddSet)))
        {
            return await LoadPageAsync(id);
        }

        if (await workoutService.AddSetAsync(id, AddSet) == AddSetEntryResult.WorkoutExerciseNotFound)
        {
            return NotFound();
        }

        TempData["StatusMessage"] = "Set added.";
        return RedirectToPage(new { id });
    }

    private async Task<IActionResult> LoadPageAsync(int id)
    {
        var workout = await workoutService.FindAsync(id);

        if (workout is null)
        {
            return NotFound();
        }

        Workout = workout;
        AvailableExercises = await workoutService.GetExercisesAsync(id);
        return Page();
    }
}
