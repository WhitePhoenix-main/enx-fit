using enx_fit.Security;
using enx_fit.Models;
using enx_fit.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Pages.Exercises;

[MinimumRole(UserRole.User)]
public class IndexModel(ExerciseService exerciseService) : PageModel
{
    public IReadOnlyList<Exercise> Exercises { get; private set; } = [];

    public IReadOnlyList<string> MuscleGroups { get; private set; } = [];
    public IReadOnlyList<int> RecentIds { get; private set; } = [];
    [BindProperty(SupportsGet = true)] public bool Reference { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SelectedMuscleGroup { get; set; }

    public async Task OnGetAsync()
    {
        Exercises = await exerciseService.GetAllAsync(SelectedMuscleGroup);
        MuscleGroups = await exerciseService.GetMuscleGroupsAsync();
        RecentIds = await exerciseService.GetRecentIdsAsync(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        ViewData["Reference"] = Reference;
    }
}
