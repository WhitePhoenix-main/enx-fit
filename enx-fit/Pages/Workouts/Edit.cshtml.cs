using enx_fit.Services;
using enx_fit.Security;
using enx_fit.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Pages.Workouts;

[MinimumRole(UserRole.User)]
public class EditModel(
    WorkoutService workoutService,
    CurrentUser currentUser,
    UserDirectoryService userDirectory) : PageModel
{
    [BindProperty]
    public int Id { get; set; }

    [BindProperty]
    public WorkoutSessionInputModel Input { get; set; } = new();

    public bool IsAdministrator => currentUser.IsAdministrator;

    public IReadOnlyList<UserOption> Users { get; private set; } = [];

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
        if (!await OwnerIsValidAsync() || !ModelState.IsValid)
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

    private async Task<bool> OwnerIsValidAsync()
    {
        if (!IsAdministrator || await userDirectory.ExistsAsync(Input.OwnerId))
        {
            return true;
        }

        ModelState.AddModelError("Input.OwnerId", "Выберите существующего пользователя.");
        return false;
    }

    private async Task LoadUsersAsync()
    {
        if (IsAdministrator)
        {
            Users = await userDirectory.GetAllAsync();
        }
    }
}
