using enx_fit.Models;
using enx_fit.Services;
using enx_fit.Security;
using enx_fit.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Pages.Workouts;

[MinimumRole(UserRole.User)]
public class DetailsModel(
    WorkoutService workoutService,
    CurrentUser currentUser,
    UserDirectoryService userDirectory,
    ExerciseService exerciseService) : PageModel
{
    public WorkoutSession Workout { get; private set; } = null!;

    public WorkoutBuilderInput? Blueprint { get; private set; }

    public ExerciseLibraryModel Library { get; private set; } = null!;

    public bool IsAdministrator => currentUser.IsAdministrator;
    public List<WorkoutTemplateOption> Templates { get; private set; } = [];
    public bool CanUpdateTemplate { get; private set; }
    [BindProperty] public int ExerciseEntryId { get; set; }

    public Task<IActionResult> OnPostLoadTemplateAsync(int id, int templateId) => MutateAsync(id, () => workoutService.LoadTemplateAsync(id, templateId));
    public Task<IActionResult> OnPostExerciseAsync(int id, string action) => MutateAsync(id, () => workoutService.ChangeExerciseAsync(id, ExerciseEntryId, action));
    public Task<IActionResult> OnPostTemplateDecisionAsync(int id, string choice, string? templateName) => MutateAsync(id, () => workoutService.DecideTemplateAsync(id, choice, templateName));
    public Task<IActionResult> OnPostStartAsync(int id) => MutateAsync(id, async () =>
    {
        var active = await workoutService.StartAsync("planned", plannedId: id);
        if (active != id) TempData["ActiveWorkoutId"] = active;
    });

    public async Task<IActionResult> OnPostSetAsync(int id, int setId, bool completed, bool remove)
    {
        ValidateOnly(nameof(AddSet));
        var ajax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        if (!remove && (!ModelState.IsValid || !TryValidateModel(AddSet, nameof(AddSet))))
            return ajax ? BadRequest(new { error = "Проверьте вес, повторения и RIR." }) : await LoadPageAsync(id);
        try
        {
            await workoutService.ChangeSetAsync(id, setId, AddSet, completed, remove);
            return ajax ? new JsonResult(new { saved = true }) : RedirectToPage(new { id });
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex)
        {
            if (ajax) return BadRequest(new { error = ex.Message });
            ModelState.AddModelError("", ex.Message);
            return await LoadPageAsync(id);
        }
    }

    private async Task<IActionResult> MutateAsync(int id, Func<Task> action)
    {
        ModelState.Clear();
        try
        {
            await action();
            if (TempData["ActiveWorkoutId"] is int active) return RedirectToPage(new { id = active });
            return RedirectToPage(new { id });
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); return await LoadPageAsync(id); }
    }

    public string OwnerName { get; private set; } = string.Empty;

    [BindProperty]
    public AddWorkoutExerciseInputModel AddExercise { get; set; } = new();

    [BindProperty]
    public AddSetEntryInputModel AddSet { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id) =>
        await LoadPageAsync(id);

    public async Task<IActionResult> OnPostCompleteAsync(int id)
    {
        ModelState.Clear();
        var result = await workoutService.CompleteAsync(id);
        if (result == CompleteWorkoutResult.NotFound) return NotFound();
        if (result == CompleteWorkoutResult.Empty)
        {
            ModelState.AddModelError("", "Запишите хотя бы один рабочий подход перед завершением.");
            return await LoadPageAsync(id);
        }
        TempData["StatusMessage"] = "Тренировка завершена. Результаты сохранены в истории.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAddExerciseAsync(int id)
    {
        ValidateOnly(nameof(AddExercise));
        if (!ModelState.IsValid || !TryValidateModel(AddExercise, nameof(AddExercise))) return await LoadPageAsync(id);
        if (ExerciseEntryId != 0)
            return await MutateAsync(id, () => workoutService.ChangeExerciseAsync(id, ExerciseEntryId, "replace", AddExercise.ExerciseId));

        var result = await workoutService.AddExerciseAsync(id, AddExercise.ExerciseId);

        if (result == AddWorkoutExerciseResult.WorkoutNotFound)
        {
            return NotFound();
        }

        if (result == AddWorkoutExerciseResult.ExerciseNotFound)
        {
            ModelState.AddModelError("AddExercise.ExerciseId", "Выберите существующее упражнение.");
            return await LoadPageAsync(id);
        }

        if (result == AddWorkoutExerciseResult.AlreadyAdded)
        {
            ModelState.AddModelError("AddExercise.ExerciseId", "Это упражнение уже есть в тренировке.");
            return await LoadPageAsync(id);
        }

        TempData["StatusMessage"] = "Упражнение добавлено в тренировку.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAddSetAsync(int id)
    {
        ValidateOnly(nameof(AddSet));

        if (!ModelState.IsValid || !TryValidateModel(AddSet, nameof(AddSet)))
        {
            return await LoadPageAsync(id);
        }

        if (await workoutService.AddSetAsync(id, AddSet) == AddSetEntryResult.WorkoutExerciseNotFound)
        {
            return NotFound();
        }

        TempData["StatusMessage"] = "Подход добавлен.";
        return RedirectToPage(new { id });
    }

    private void ValidateOnly(string prefix)
    {
        foreach (var key in ModelState.Keys.Where(k => !k.StartsWith(prefix + ".", StringComparison.Ordinal)).ToArray())
            ModelState.Remove(key);
    }

    private async Task<IActionResult> LoadPageAsync(int id)
    {
        var workout = await workoutService.FindAsync(id);

        if (workout is null)
        {
            return NotFound();
        }

        Workout = workout;
        Templates = await workoutService.TemplatesAsync();
        CanUpdateTemplate = await workoutService.CanUpdateTemplateAsync(workout);
        Response.Headers.CacheControl = "no-cache, no-store";
        if (workout.BuilderConfigurationJson is not null && WorkoutBuilderInput.TryParse(workout.BuilderConfigurationJson, out var blueprint))
            Blueprint = blueprint;
        if (IsAdministrator)
        {
            OwnerName = await userDirectory.GetDisplayNameAsync(workout.UserId);
        }
        Library = new ExerciseLibraryModel
        {
            Id = "workout-library", ViewerId = currentUser.Id,
            Exercises = await exerciseService.GetAllAsync(null),
            SelectedIds = workout.WorkoutExercises.Select(e => e.ExerciseId).ToArray(),
            RecentIds = await exerciseService.GetRecentIdsAsync(workout.UserId ?? string.Empty)
        };
        return Page();
    }
}
