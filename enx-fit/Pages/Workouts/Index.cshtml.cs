using enx_fit.Models;
using enx_fit.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Pages.Workouts;

public class IndexModel(WorkoutService workoutService) : PageModel
{
    public IReadOnlyList<WorkoutSession> Workouts { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Workouts = await workoutService.GetAllAsync();
    }
}
