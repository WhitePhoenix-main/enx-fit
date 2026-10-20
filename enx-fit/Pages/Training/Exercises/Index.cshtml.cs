using enx_fit.Models;
using enx_fit.Security;
using enx_fit.Services.Exercises;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Pages.Training.Exercises;

[MinimumRole(UserRole.User)]
public class IndexModel(ExerciseService exerciseService, enx_fit.Services.Dashboard.DashboardService dashboard,
    enx_fit.Data.ApplicationDbContext db, enx_fit.Security.CurrentUser currentUser, enx_fit.Security.IFeatureAccessService features) : PageModel
{
    public IReadOnlyList<enx_fit.Services.Dashboard.ClientSummary> Clients { get; private set; } = [];
    public DateOnly Today => currentUser.LocalToday;
    public IReadOnlyList<Exercise> Exercises { get; private set; } = [];

    public IReadOnlyList<string> MuscleGroups { get; private set; } = [];
    public IReadOnlyList<Guid> RecentIds { get; private set; } = [];
    [BindProperty(SupportsGet = true)] public bool Reference { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SelectedMuscleGroup { get; set; }

    public async Task OnGetAsync()
    {
        Exercises = await exerciseService.GetAllAsync(SelectedMuscleGroup);
        var featured = new[] { "жим штанги лежа", "приседания со штангой", "подтягивания", "тяга гантели в наклоне" };
        var sorted = Exercises.OrderBy(e => e.Name).ToArray();
        var popular = featured.Select(name => sorted.FirstOrDefault(e =>
            enx_fit.Presentation.Exercises.ExercisePresentation.DisplayName(e).ToLowerInvariant().Replace('ё','е') == name))
            .OfType<Exercise>().ToArray();
        Exercises = popular.Concat(sorted.ExceptBy(popular.Select(e => e.Id), e => e.Id)).ToArray();
        MuscleGroups = await exerciseService.GetMuscleGroupsAsync();
        RecentIds = await exerciseService.GetRecentIdsAsync(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        if (await features.CanUseAsync(enx_fit.Security.Feature.CoachProgramAssignment)) Clients = await dashboard.GetClientsAsync();
        ViewData["Reference"] = Reference;
    }

    public async Task<IActionResult> OnPostAssignAsync(Guid exerciseId, string clientId, DateOnly date)
    {
        if (!await features.CanUseAsync(enx_fit.Security.Feature.CoachProgramAssignment)) return Forbid();
        var client = await dashboard.GetSubjectAsync(clientId);
        if (client is null || client.TrainerId != currentUser.Id) return NotFound();
        if (date < currentUser.LocalToday || date > currentUser.LocalToday.AddYears(1)) return BadRequest();
        var exercise = await db.Exercises.FindAsync(exerciseId);
        if (exercise is null) return NotFound();
        db.WorkoutSessions.Add(new() { UserId = client.Id, Date = date, Title = enx_fit.Presentation.Exercises.ExercisePresentation.DisplayName(exercise),
            Status = WorkoutStatus.Planned, WorkoutExercises = [new() { ExerciseId = exercise.Id, Order = 0, TargetSets = 3, TargetRepsMin = 8, TargetRepsMax = 12, TargetRestSeconds = 90 }] });
        await db.SaveChangesAsync();
        TempData["StatusMessage"] = $"Упражнение назначено: {client.UserName}, {date:dd.MM.yyyy}.";
        return RedirectToPage();
    }
}
