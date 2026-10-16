using enx_fit.Pages.Infrastructure;
using enx_fit.Security;
using enx_fit.Services.Accounts;
using enx_fit.Services.Workouts;
using enx_fit.ViewModels.Workouts;
using Microsoft.AspNetCore.Mvc;

namespace enx_fit.Pages.Training.Workouts;

[MinimumRole(UserRole.User)]
public class EditModel(
    WorkoutService workoutService,
    CurrentUser currentUser,
    UserDirectoryService userDirectory) : OwnerFormPageModel(currentUser, userDirectory)
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
        await LoadUsersAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await OwnerIsValidAsync(Input.OwnerId) || !ModelState.IsValid)
        {
            await LoadUsersAsync();
            return Page();
        }

        if (!await workoutService.UpdateAsync(Id, Input))
        {
            return NotFound();
        }

        TempData["StatusMessage"] = "Тренировка обновлена.";
        return RedirectToPage("./Details", new { id = Id });
    }
}
