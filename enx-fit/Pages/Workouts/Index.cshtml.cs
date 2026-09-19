using enx_fit.Models;
using enx_fit.Services;
using enx_fit.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Pages.Workouts;

[MinimumRole(UserRole.User)]
public class IndexModel(
    WorkoutService workoutService,
    CurrentUser currentUser,
    UserDirectoryService userDirectory) : PageModel
{
    public IReadOnlyList<WorkoutSession> Workouts { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public string? UserId { get; set; }

    public bool IsAdministrator => currentUser.IsAdministrator;

    public IReadOnlyList<UserOption> Users { get; private set; } = [];

    public IReadOnlyDictionary<string, string> OwnerNames { get; private set; } =
        new Dictionary<string, string>();

    public async Task OnGetAsync()
    {
        Workouts = await workoutService.GetAllAsync(UserId);
        if (IsAdministrator)
        {
            Users = await userDirectory.GetAllAsync();
            OwnerNames = await userDirectory.GetDisplayNamesAsync(Workouts.Select(workout => workout.UserId));
        }
    }

    public string OwnerName(string? ownerId) =>
        string.IsNullOrWhiteSpace(ownerId)
            ? "Не назначен"
            : OwnerNames.GetValueOrDefault(ownerId, "Удалённый пользователь");
}
