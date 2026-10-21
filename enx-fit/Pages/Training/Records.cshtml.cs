using enx_fit.Models;
using enx_fit.Security;
using enx_fit.Services.Workouts;
using enx_fit.Services.Exercises;
using enx_fit.ViewModels.Workouts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Pages.Training;

[MinimumRole(UserRole.User)]
public class RecordsModel(WorkoutService workouts, ExerciseService exercises) : PageModel
{
    public IReadOnlyList<ExercisePerformance> Records { get; private set; } = [];
    public Exercise? Selected { get; private set; }
    public async Task<IActionResult> OnGetAsync(Guid? exerciseId)
    {
        Records = await workouts.PerformanceAsync(exerciseId);
        if (exerciseId.HasValue)
        {
            Selected = await exercises.FindAsync(exerciseId.Value);
            if (Selected is null) return NotFound();
        }
        Response.Headers.CacheControl = "no-store";
        return Page();
    }
    public async Task<IActionResult> OnGetExerciseAsync(Guid exerciseId, int? beforeWorkoutId)
    {
        Response.Headers.CacheControl = "no-store";
        try {
            var performance = (await workouts.PerformanceAsync(exerciseId, beforeWorkoutId)).FirstOrDefault();
            return new JsonResult(new { performance });
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }
}
