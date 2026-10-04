using enx_fit.Models;
using enx_fit.Security;
using enx_fit.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using enx_fit.ViewModels;

namespace enx_fit.Pages.Programs;

public sealed class DetailsModel(TrainingProgramService programs, CurrentUser user,
    ProgramTrainingDataService data, IRecommendationService recommendations) : ProgramPageModel(programs)
{
    public TrainingProgram Item { get; private set; } = null!;
    public List<WorkoutSession> Sessions { get; private set; } = [];
    public List<ProgramClientStatus> Clients { get; private set; } = [];
    public ProgramOccurrence? Next => ProgramSchedule.Next(Item, Sessions, user.LocalToday);
    public bool IsOwner => Item.OwnerId == user.Id;
    public int Total => ProgramSchedule.Occurrences(Item).Count();
    public int Done => ProgramSchedule.Done(Item, Sessions);
    public int Percent => Total > 0 ? Math.Min(100, Done * 100 / Total) : 0;
    public DateOnly Today => user.LocalToday;
    [BindProperty(SupportsGet = true)] public DateOnly? Week { get; set; }
    [BindProperty(SupportsGet = true)] public DateOnly? Day { get; set; }
    public ProgramCalendarViewModel Calendar => new(Item, Sessions, Today, Week);
    [BindProperty(SupportsGet = true)] public string? Tab { get; set; } = "overview";
    [BindProperty(SupportsGet = true)] public int? ExerciseId { get; set; }
    [BindProperty] public ProgressionRule Rule { get; set; } = new();
    [BindProperty] public ExercisePrescription Prescription { get; set; } = new();
    [BindProperty] public Guid Revision { get; set; }
    public List<ExercisePerformance> ExerciseHistory { get; private set; } = [];
    public List<TrainingRecommendation> Recommendations { get; private set; } = [];
    public bool CanAnalyze => Access.CanUse(IsOwner ? Feature.ProgressionRecommendations : Feature.CoachAnalytics);
    public ProgramWorkoutExercise? SelectedExercise => Item.Workouts.OrderBy(w => w.Order).SelectMany(w => w.Exercises.OrderBy(e => e.Order)).FirstOrDefault(e => e.Id == ExerciseId)
        ?? Item.Workouts.OrderBy(w => w.Order).SelectMany(w => w.Exercises.OrderBy(e => e.Order)).FirstOrDefault();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        await LoadAsync(id);
        if (PageError is null && SelectedExercise is { } e) { ExerciseId = e.Id; Rule = e.Progression; Prescription = e.Prescription; Revision = Item.Revision; }
        return Page();
    }
    private async Task LoadAsync(int id)
    {
        await LoadAccessAsync();
        if (Tab is not ("overview" or "schedule" or "workouts" or "progression" or "clients")) Tab = "overview";
        var item = await Programs.FindAsync(id);
        if (item is null || item.IsTemplate) { Error(new(ProgramFailure.NotFound)); return; }
        Item = item; Sessions = await Programs.SessionsAsync(Item);
        await SetClientContextAsync(Item);
        if (Tab == "progression" && SelectedExercise is { } selected)
        {
            ExerciseHistory = await data.HistoryAsync(id, selected.Id);
            if (CanAnalyze) Recommendations = await recommendations.ListAsync(id, selected.Id);
        }
        if (Tab == "clients")
        {
            if (!Access.IsCoach) { Error(new(ProgramFailure.Forbidden)); return; }
            if (Access.CanUse(Feature.CoachProgramAssignment))
            {
                var clients = await Programs.ClientsAsync();
                foreach (var p in await Programs.AssignmentsAsync(id))
                    Clients.Add(new(p, clients.FirstOrDefault(c => c.Id == p.OwnerId)?.Name ?? "Клиент", await Programs.SessionsAsync(p)));
            }
        }
    }
    public async Task<IActionResult> OnPostActivateAsync(int id, DateOnly? startDate)
    {
        Tab = "schedule";
        if (!ModelState.IsValid) { await LoadAsync(id); return Page(); }
        return await Mutate(id, async () => { await Programs.ActivateAsync(id, startDate ?? Today); return RedirectToPage(new { id, Tab, Week = (startDate ?? Today).ToString("yyyy-MM-dd") }); });
    }
    public async Task<IActionResult> OnPostMoveAsync(int id, Guid workoutKey, DateOnly originalDate, DateOnly date, Guid scheduleRevision)
    {
        Tab = "schedule";
        if (!ModelState.IsValid) { await LoadAsync(id); return Page(); }
        return await Mutate(id, async () =>
        {
            await Programs.MoveOccurrenceAsync(id, workoutKey, originalDate, date, scheduleRevision);
            TempData["StatusMessage"] = "Занятие перенесено. Другие дни программы сохранились.";
            return RedirectToPage(new { id, Tab, Week = date.ToString("yyyy-MM-dd"), Day = date.ToString("yyyy-MM-dd") });
        });
    }
    public async Task<IActionResult> OnPostStartOccurrenceAsync(int id, Guid workoutKey, DateOnly originalDate)
    {
        Tab = "schedule";
        if (!ModelState.IsValid) { await LoadAsync(id); return Page(); }
        return await Mutate(id, async () => RedirectToPage("/Workouts/Details", new { id = await Programs.StartOccurrenceAsync(id, workoutKey, originalDate) }));
    }
    public Task<IActionResult> OnPostStartAsync(int id) => Mutate(id, async () =>
        RedirectToPage("/Workouts/Details", new { id = await Programs.StartNextAsync(id) }));
    public Task<IActionResult> OnPostDuplicateAsync(int id) => Mutate(id, async () =>
        RedirectToPage(new { id = await Programs.DuplicateAsync(id) }));
    public Task<IActionResult> OnPostArchiveAsync(int id, bool restore) => Mutate(id, async () =>
    {
        await Programs.ArchiveAsync(id, restore); TempData["StatusMessage"] = restore ? "Программа восстановлена." : "Программа в архиве. История тренировок сохранена.";
        return RedirectToPage(new { id });
    });
    public async Task<IActionResult> OnPostProgressionAsync(int id)
    {
        Tab = "progression";
        await LoadAsync(id);
        if (PageError is not null || !ModelState.IsValid) return Page();
        return await Mutate(id, async () =>
        {
            await Programs.SaveProgressionAsync(id, ExerciseId ?? 0, Rule);
            TempData["StatusMessage"] = "Правило прогрессии сохранено.";
            return RedirectToPage(new { id, Tab, ExerciseId });
        });
    }
    public async Task<IActionResult> OnPostPrescriptionAsync(int id)
    {
        Tab = "progression";
        await LoadAsync(id);
        if (PageError is not null || !ModelState.IsValid) return Page();
        return await Mutate(id, async () =>
        {
            await Programs.SavePrescriptionAsync(id, ExerciseId ?? 0, Prescription, Revision);
            TempData["StatusMessage"] = "Плановая нагрузка сохранена. Выполненные подходы остались без изменений.";
            return RedirectToPage(new { id, Tab, ExerciseId });
        });
    }

    public Task<IActionResult> OnPostAnalyzeAsync(int id) => Mutate(id, async () =>
    {
        Tab = "progression";
        TempData["StatusMessage"] = await recommendations.GenerateAsync(id, ExerciseId ?? 0) ?? "Анализ обновлён. Проверьте рекомендацию перед применением.";
        return RedirectToPage(new { id, Tab, ExerciseId });
    });

    public Task<IActionResult> OnPostRecommendationAsync(int id, int recommendationId, bool accept, bool confirmed) => Mutate(id, async () =>
    {
        Tab = "progression";
        await recommendations.ResolveAsync(id, recommendationId, accept, confirmed);
        TempData["StatusMessage"] = accept ? "Рекомендация применена к следующим тренировкам." : "Рекомендация отклонена. Нагрузка не изменена.";
        return RedirectToPage(new { id, Tab, ExerciseId });
    });

    private async Task<IActionResult> Mutate(int id, Func<Task<IActionResult>> action)
    {
        await LoadAccessAsync();
        try { return await action(); }
        catch (ProgramOperationException e) { await LoadAsync(id); return Error(e); }
        catch (DbUpdateException e) { SaveError(e); await LoadAsync(id); return Page(); }
        catch (System.Data.Common.DbException e) { SaveError(e); await LoadAsync(id); return Page(); }
    }
}
public sealed record ProgramClientStatus(TrainingProgram Program, string Name, List<WorkoutSession> Sessions)
{
    public int Percent => ProgramSchedule.Occurrences(Program).Any() ? Math.Min(100, ProgramSchedule.Done(Program, Sessions) * 100 / ProgramSchedule.Occurrences(Program).Count()) : 0;
    public DateOnly? Last => Sessions.Where(s => s.State == WorkoutStatus.Completed).Max(s => (DateOnly?)s.Date);
    public string Status => Program.StartDate > DateOnly.FromDateTime(DateTime.UtcNow) ? "Ожидает старта" :
        ProgramSchedule.Occurrences(Program).Any(o => o.Date < DateOnly.FromDateTime(DateTime.UtcNow) &&
            !Sessions.Any(s => ProgramSchedule.Matches(s, o) && s.State == WorkoutStatus.Completed)) ? "Есть пропуски" : "Всё по плану";
}
