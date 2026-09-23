using enx_fit.Models;
using enx_fit.Services;
using enx_fit.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace enx_fit.Pages.Programs;

[RequestFormLimits(ValueCountLimit = 20000)]
public sealed class EditModel(TrainingProgramService programs) : ProgramPageModel(programs)
{
    [BindProperty] public ProgramInput Input { get; set; } = new();
    public List<Exercise> Exercises { get; private set; } = [];
    public int? ProgramId { get; private set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (!await LoadAsync(id)) return Page();
        if (id.HasValue) Input = ProgramInput.From((await Programs.FindAsync(id.Value))!);
        return Page();
    }

    private async Task<bool> LoadAsync(int? id)
    {
        ProgramId = id;
        await LoadAccessAsync();
        if (id.HasValue)
        {
            var program = await Programs.FindAsync(id.Value);
            if (program is null) { Error(new(ProgramFailure.NotFound)); return false; }
            if (program.IsTemplate || program.IsArchived) { Error(new(ProgramFailure.Forbidden)); return false; }
        }
        Exercises = await Programs.ExercisesAsync();
        return true;
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (!await LoadAsync(id) || !ModelState.IsValid) return Page();
        try
        {
            var savedId = await Programs.SaveAsync(id, Input);
            TempData["StatusMessage"] = "Программа сохранена.";
            return RedirectToPage("Details", new { id = savedId });
        }
        catch (ProgramOperationException e) { return Error(e); }
        catch (DbUpdateException e) { SaveError(e); return Page(); }
    }

    // Structural edits are a preview. Only the main Save button persists the program.
    public async Task<IActionResult> OnPostStructureAsync(int? id, string command, int workout = -1, int exercise = -1)
    {
        if (!await LoadAsync(id)) return Page();
        if (Input.Workouts.Count > 14 || Input.Workouts.Any(w => w.Exercises.Count > 30)) return BadRequest();
        ModelState.Clear();
        var w = Input.Workouts.ElementAtOrDefault(workout);
        switch (command)
        {
            case "add-workout" when Input.Workouts.Count < 14: Input.Workouts.Add(new()); break;
            case "remove-workout" when w is not null: Input.Workouts.RemoveAt(workout); break;
            case "duplicate-workout" when w is not null && Input.Workouts.Count < 14:
                var copy = System.Text.Json.JsonSerializer.Deserialize<ProgramWorkoutInput>(System.Text.Json.JsonSerializer.Serialize(w))!;
                copy.Key = Guid.Empty; copy.DayOfWeek = null; copy.Name = w.Name[..Math.Min(110, w.Name.Length)] + " · копия";
                Input.Workouts.Insert(workout + 1, copy); break;
            case "up-workout" when workout > 0 && w is not null:
                (Input.Workouts[workout - 1], Input.Workouts[workout]) = (w, Input.Workouts[workout - 1]); break;
            case "down-workout" when w is not null && workout < Input.Workouts.Count - 1:
                (Input.Workouts[workout + 1], Input.Workouts[workout]) = (w, Input.Workouts[workout + 1]); break;
            case "add-exercise" when w is not null && w.Exercises.Count < 30:
                var available = Exercises.FirstOrDefault(e => w.Exercises.All(x => x.ExerciseId != e.Id));
                if (available is not null) w.Exercises.Add(new() { ExerciseId = available.Id }); break;
            case "remove-exercise" when w is not null && exercise >= 0 && exercise < w.Exercises.Count:
                w.Exercises.RemoveAt(exercise); break;
            case "up-exercise" when w is not null && exercise > 0 && exercise < w.Exercises.Count:
                (w.Exercises[exercise - 1], w.Exercises[exercise]) = (w.Exercises[exercise], w.Exercises[exercise - 1]); break;
            case "down-exercise" when w is not null && exercise >= 0 && exercise < w.Exercises.Count - 1:
                (w.Exercises[exercise + 1], w.Exercises[exercise]) = (w.Exercises[exercise], w.Exercises[exercise + 1]); break;
        }
        return Page();
    }
}
