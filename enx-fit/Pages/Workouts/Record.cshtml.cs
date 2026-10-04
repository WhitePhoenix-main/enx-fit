using System.ComponentModel.DataAnnotations;
using enx_fit.Security;
using enx_fit.Services;
using enx_fit.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Pages.Workouts;

[MinimumRole(UserRole.User)]
public sealed class RecordModel(WorkoutService workouts, ExerciseService exercises, CurrentUser currentUser) : PageModel
{
    [BindProperty] public RecordWorkoutInput Input { get; set; } = new();
    public ExerciseLibraryModel Library { get; private set; } = null!;
    public List<WorkoutTemplateOption> Templates { get; private set; } = [];
    public string ViewerId => currentUser.Id;
    public DateOnly Today => currentUser.LocalToday;
    public async Task OnGetAsync()
    {
        Input.Date = Today; Input.UtcOffsetMinutes = currentUser.UtcOffsetMinutes;
        await LoadAsync();
    }
    public async Task<IActionResult> OnGetSourceAsync(string source, int? templateId)
    {
        try { var result = await workouts.RecordSourceAsync(source, templateId); return new JsonResult(new { result.Title, result.Exercises }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }
    public async Task<IActionResult> OnPostAsync()
    {
        if (ModelState.IsValid)
        {
            try
            {
                var id = await workouts.RecordAsync(Input);
                TempData["RecordedWorkout"] = Input.ClientRequestId.ToString();
                TempData["StatusMessage"] = "Прошедшая тренировка сохранена в истории.";
                return RedirectToPage("Details", new { id });
            }
            catch (Exception ex) when (ex is InvalidOperationException or ValidationException) { ModelState.AddModelError("", ex.Message); }
        }
        await LoadAsync(); return Page();
    }
    private async Task LoadAsync()
    {
        Response.Headers.CacheControl = "no-store";
        Library = new() { Id = "record-library", ViewerId = ViewerId, ForSelection = true,
            Exercises = await exercises.GetAllAsync(null), RecentIds = await exercises.GetRecentIdsAsync(ViewerId) };
        Templates = await workouts.TemplatesAsync();
    }
}
