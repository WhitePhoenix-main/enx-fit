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
    [BindProperty(SupportsGet = true)] public DateOnly? Date { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    public int FilteredCount { get; private set; }
    public int PageCount { get; private set; } = 1;

    public bool IsAdministrator => currentUser.IsAdministrator;

    public IReadOnlyList<UserOption> Users { get; private set; } = [];

    public IReadOnlyDictionary<string, string> OwnerNames { get; private set; } =
        new Dictionary<string, string>();

    public async Task OnGetAsync()
    {
        Response.Headers.CacheControl = "no-cache, no-store";
        if (IsAdministrator)
        {
            var page = await workoutService.DirectoryAsync(UserId, Date, PageNumber);
            Workouts = page.Items; FilteredCount = page.Count; PageNumber = page.PageNumber; PageCount = page.PageCount;
            Users = await userDirectory.GetAllAsync();
            OwnerNames = await userDirectory.GetDisplayNamesAsync(Workouts.Select(workout => workout.UserId));
        }
        else { Workouts = await workoutService.GetAllAsync(); FilteredCount = Workouts.Count; }
        ModelState.Clear();
    }
    public static string StateName(WorkoutSession session) => session.State switch {
        WorkoutStatus.Completed => "Завершена", WorkoutStatus.InProgress => "В процессе", WorkoutStatus.Paused => "На паузе",
        WorkoutStatus.Cancelled => "Отменена", _ => "Запланирована" };

    public string OwnerName(string? ownerId) =>
        string.IsNullOrWhiteSpace(ownerId)
            ? "Не назначен"
            : OwnerNames.GetValueOrDefault(ownerId, "Удалённый пользователь");
}
