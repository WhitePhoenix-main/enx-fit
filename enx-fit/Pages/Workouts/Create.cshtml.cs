using enx_fit.Services;
using enx_fit.Security;
using enx_fit.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Pages.Workouts;

[MinimumRole(UserRole.User)]
public class CreateModel(
    WorkoutService workoutService,
    CurrentUser currentUser,
    UserDirectoryService userDirectory) : PageModel
{
    [BindProperty]
    public WorkoutSessionInputModel Input { get; set; } = new();

    public bool IsAdministrator => currentUser.IsAdministrator;

    public IReadOnlyList<UserOption> Users { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(string? userId)
    {
        Input.OwnerId = currentUser.Id;
        if (IsAdministrator && !string.IsNullOrWhiteSpace(userId))
        {
            if (!await userDirectory.ExistsAsync(userId)) return NotFound();
            Input.OwnerId = userId;
        }
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

        var id = await workoutService.CreateAsync(Input);
        TempData["StatusMessage"] = "Тренировка создана.";
        return RedirectToPage("./Details", new { id });
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
