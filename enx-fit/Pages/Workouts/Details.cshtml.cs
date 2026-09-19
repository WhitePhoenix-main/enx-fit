using enx_fit.Models;
using enx_fit.Services;
using enx_fit.Security;
using enx_fit.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Pages.Workouts;

[MinimumRole(UserRole.User)]
public class DetailsModel(
    WorkoutService workoutService,
    CurrentUser currentUser,
    UserDirectoryService userDirectory) : PageModel
{
    public WorkoutSession Workout { get; private set; } = null!;

    public IReadOnlyList<Exercise> AvailableExercises { get; private set; } = [];

    public bool IsAdministrator => currentUser.IsAdministrator;

    public string OwnerName { get; private set; } = string.Empty;

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
            ModelState.AddModelError("AddExercise.ExerciseId", "Выберите существующее упражнение.");
            return await LoadPageAsync(id);
        }

        if (result == AddWorkoutExerciseResult.AlreadyAdded)
        {
            ModelState.AddModelError("AddExercise.ExerciseId", "Это упражнение уже есть в тренировке.");
            return await LoadPageAsync(id);
        }

        TempData["StatusMessage"] = "Упражнение добавлено в тренировку.";
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

        TempData["StatusMessage"] = "Подход добавлен.";
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
        if (IsAdministrator)
        {
            OwnerName = await userDirectory.GetDisplayNameAsync(workout.UserId);
        }
        AvailableExercises = await workoutService.GetExercisesAsync(id);
        return Page();
    }
}
