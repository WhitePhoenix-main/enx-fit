using enx_fit.Models;
using enx_fit.Services;
using enx_fit.Security;
using enx_fit.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;

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

    public IReadOnlyDictionary<Guid, PreviousExerciseResult> PreviousResults { get; private set; } = new Dictionary<Guid, PreviousExerciseResult>();

    public ExerciseLibraryModel Library { get; private set; } = null!;

    public bool IsAdministrator => currentUser.IsAdministrator;
    public List<WorkoutTemplateOption> Templates { get; private set; } = [];
    public bool CanUpdateTemplate { get; private set; }
    public WorkoutExecutionState Execution { get; private set; } = null!;
    public string ViewerId => currentUser.Id;
    [BindProperty] public int ExerciseEntryId { get; set; }

    public async Task<IActionResult> OnPostPickExercisesAsync(int id, string selectionJson, Guid? operationId, Guid? expectedRevision)
    {
        ModelState.Clear();
        try
        {
            if (selectionJson is null || selectionJson.Length > 16000) return BadRequest(new { error = "Выберите упражнения из библиотеки." });
            var picks = JsonSerializer.Deserialize<List<ExercisePickInput>>(selectionJson, WorkoutBuilderInput.JsonOptions);
            if (picks is null || picks.Any(p => p is null)) return BadRequest(new { error = "Проверьте выбранные упражнения." });
            await workoutService.PickExercisesAsync(id, picks, ExerciseEntryId, operationId, expectedRevision);
            return await LoadPageAsync(id);
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (WorkoutConflictException ex) { return StatusCode(409, new { error = ex.Message }); }
        catch (JsonException) { return BadRequest(new { error = "Проверьте параметры упражнений." }); }
        catch (ValidationException) { return BadRequest(new { error = "Подходы: 1–20, повторы: 1–1000, вес: 0–2000 кг, отдых: 0–900 секунд." }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    public Task<IActionResult> OnPostLoadTemplateAsync(int id, int templateId) => MutateAsync(id, () => workoutService.LoadTemplateAsync(id, templateId));
    public Task<IActionResult> OnPostExerciseAsync(int id, string action, Guid? operationId, Guid? expectedRevision) =>
        MutateAsync(id, () => workoutService.ChangeExerciseAsync(id, ExerciseEntryId, action, operationId: operationId, expectedRevision: expectedRevision));
    public async Task<IActionResult> OnPostReorderAsync(int id, string orderJson, Guid? operationId, Guid? expectedRevision)
    {
        ModelState.Clear();
        try
        {
            if (orderJson is null || orderJson.Length > 1000) return BadRequest(new { error = "Проверьте порядок упражнений." });
            var ids = JsonSerializer.Deserialize<List<int>>(orderJson);
            if (ids is null) return BadRequest(new { error = "Проверьте порядок упражнений." });
            return new JsonResult(await workoutService.ReorderExercisesAsync(id, ids, operationId, expectedRevision));
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (WorkoutConflictException ex) { return StatusCode(409, new { error = ex.Message }); }
        catch (JsonException) { return BadRequest(new { error = "Проверьте порядок упражнений." }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }
    public Task<IActionResult> OnPostTemplateDecisionAsync(int id, string choice, string? templateName, Guid? operationId) => MutateAsync(id, async () =>
    {
        await workoutService.DecideTemplateAsync(id, choice, templateName, operationId);
        TempData["StatusMessage"] = choice == "new" ? "Шаблон сохранён. Он доступен в выборе следующей тренировки." : choice == "update" ? "Шаблон обновлён." : "Результаты сохранены. Шаблон оставлен без изменений.";
    });
    public Task<IActionResult> OnPostStartAsync(int id) => MutateAsync(id, async () =>
    {
        var active = await workoutService.StartAsync("planned", plannedId: id);
        if (active != id) TempData["ActiveWorkoutId"] = active;
    });

    public async Task<IActionResult> OnGetStateAsync(int id)
    {
        Response.Headers.CacheControl = "no-store";
        try { return new JsonResult(await workoutService.ExecutionStateAsync(id)); }
        catch (KeyNotFoundException) { return NotFound(); }
    }
    public async Task<IActionResult> OnPostControlAsync(int id, string action, Guid? operationId, Guid? expectedRevision, int seconds, DateTime? performedAtUtc)
    {
        try { return new JsonResult(await workoutService.ControlAsync(id, action, operationId, expectedRevision, seconds, performedAtUtc)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (WorkoutConflictException ex) { return StatusCode(409, new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }
    public async Task<IActionResult> OnPostSetAsync(int id, int setId, bool completed, bool remove,
        Guid? operationId, Guid? expectedRevision, DateTime? performedAtUtc, bool skipped)
    {
        ValidateOnly(nameof(AddSet));
        var ajax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        if (!remove && (!ModelState.IsValid || !TryValidateModel(AddSet, nameof(AddSet))))
            return ajax ? BadRequest(new { error = "Проверьте вес, повторения и RIR." }) : await LoadPageAsync(id);
        try
        {
            var state = await workoutService.ChangeSetAsync(id, setId, AddSet, completed, remove, operationId, expectedRevision, performedAtUtc, skipped);
            return ajax ? new JsonResult(state) : RedirectToPage(new { id });
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (WorkoutConflictException ex) { return StatusCode(409, new { error = ex.Message }); }
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

    public async Task<IActionResult> OnPostCompleteAsync(int id, Guid? operationId, Guid? expectedRevision)
    {
        ModelState.Clear();
        CompleteWorkoutResult result;
        try { result = await workoutService.CompleteAsync(id, operationId, expectedRevision); }
        catch (WorkoutConflictException ex) { ModelState.AddModelError("", ex.Message); return await LoadPageAsync(id); }
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

        AddWorkoutExerciseResult result;
        try { result = await workoutService.AddExerciseAsync(id, AddExercise.ExerciseId); }
        catch (WorkoutConflictException ex) { ModelState.AddModelError("", ex.Message); return await LoadPageAsync(id); }

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

    public async Task<IActionResult> OnPostAddSetAsync(int id, bool? completed)
    {
        ValidateOnly(nameof(AddSet));

        if (!ModelState.IsValid || !TryValidateModel(AddSet, nameof(AddSet)))
        {
            return await LoadPageAsync(id);
        }

        AddSetEntryResult result;
        try { result = await workoutService.AddSetAsync(id, AddSet, completed ?? true); }
        catch (WorkoutConflictException ex) { ModelState.AddModelError("", ex.Message); return await LoadPageAsync(id); }
        if (result == AddSetEntryResult.WorkoutExerciseNotFound)
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
        PreviousResults = await workoutService.PreviousExerciseResultsAsync(workout);
        Execution = WorkoutService.State(workout);
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
            ContextName = workout.Title ?? "Текущая тренировка",
            Exercises = await exerciseService.GetAllAsync(null),
            SelectedIds = workout.WorkoutExercises.Select(e => e.ExerciseId).ToArray(),
            RecentIds = await exerciseService.GetRecentIdsAsync(workout.UserId ?? string.Empty)
        };
        return Page();
    }
}
