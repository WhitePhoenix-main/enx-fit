using System.ComponentModel.DataAnnotations;
using enx_fit.Data;
using enx_fit.Models;
using enx_fit.Security;
using enx_fit.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace enx_fit.Pages;

[MinimumRole(UserRole.User)]
[RequestSizeLimit(16384)]
public class DashboardModel(DashboardService dashboard, ApplicationDbContext db, CurrentUser currentUser,
    IFeatureAccessService features, CoachAttentionService attention, TrainingProgramService programs, WorkoutService workouts,
    IRecommendationService recommendations) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? ClientId { get; set; }
    [BindProperty(SupportsGet = true)] public int Days { get; set; } = 7;
    [BindProperty(SupportsGet = true)] public DateOnly? Until { get; set; }
    [BindProperty(SupportsGet = true)] public int WeekOffset { get; set; }
    [BindProperty(SupportsGet = true)] public int ChartWeeks { get; set; } = 6;
    // An explicit visual snapshot reproduces the reference content without altering user records.
    [BindProperty(SupportsGet = true)] public bool Reference { get; set; }
    [BindProperty] public GoalInput Goal { get; set; } = new();
    [BindProperty] public CheckInInput CheckIn { get; set; } = new();
    public DashboardData Data { get; private set; } = null!;
    public IReadOnlyList<ClientSummary> Clients { get; private set; } = [];
    public CoachAttention? Attention { get; private set; }
    public TrainingProgram? ActiveProgram { get; private set; }
    public ProgramOccurrence? NextProgramWorkout { get; private set; }
    public bool CanAnalyzePrograms { get; private set; }
    public TrainingRecommendation? NextRecommendation { get; private set; }
    public ProgramWorkoutExercise? NextGoalExercise { get; private set; }
    public WorkoutSession? ActiveWorkout { get; private set; }
    public List<WorkoutTemplateOption> Templates { get; private set; } = [];
    public bool ShowOnboarding => !IsCoachView && ActiveWorkout is null && !Data.Completed.Any() && Data.Measurements.Count == 0;
    public bool UsesDefaultLayout { get; private set; }

    public async Task<IActionResult> OnPostStartAsync(string source, int? templateId, int? plannedId, int? programId)
    {
        if (!await LoadAsync()) return NotFound();
        if (IsCoachView) return Forbid();
        try
        {
            var id = source == "program" && programId.HasValue
                ? await programs.StartNextAsync(programId.Value)
                : await workouts.StartAsync(source, templateId, plannedId);
            return RedirectToPage("/Workouts/Details", new { id });
        }
        catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); }
        catch (ProgramOperationException ex) { ModelState.AddModelError("", ex.Message); }
        return Page();
    }
    public List<DashboardWidgetPlacement> Widgets { get; private set; } = [];
    public string CurrentPage => PageContext.ActionDescriptor.ViewEnginePath;
    public DashboardSection? Section => DashboardSections.Find(CurrentPage);
    public object NavigationValues => new { ClientId = IsCoachView ? Data.Subject.Id : null, Days, Until = Until?.ToString("yyyy-MM-dd") };
    public bool IsCoachView => Data.Subject.Id != currentUser.Id;
    public bool IsTrainer => currentUser.HasMinimumRole(UserRole.Trainer);
    [TempData] public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await LoadAsync()) return NotFound();
        Goal = new() { Title = Data.Settings.GoalTitle, WeeklyWorkouts = Data.Settings.WeeklyWorkoutGoal, TrainerNote = Data.Settings.TrainerNote };
        var today = Data.TodayCheckIn;
        CheckIn = new() { WaterMl = today.WaterMl, Steps = today.Steps, SleepMinutes = today.SleepMinutes, ReadingMinutes = today.ReadingMinutes, NutritionLogged = today.NutritionLogged, StretchingDone = today.StretchingDone };
        return Page();
    }

    public async Task<IActionResult> OnPostGoalAsync()
    {
        if (!await LoadAsync()) return NotFound();
        if (!ModelState.IsValid) return Page();
        var settings = await db.DashboardSettings.FindAsync(Data.Subject.Id);
        if (settings is null) { settings = new DashboardSettings { UserId = Data.Subject.Id }; db.DashboardSettings.Add(settings); }
        settings.GoalTitle = Goal.Title.Trim();
        settings.WeeklyWorkoutGoal = Goal.WeeklyWorkouts;
        if (IsCoachView) settings.TrainerNote = Goal.TrainerNote?.Trim();
        await db.SaveChangesAsync();
        StatusMessage = "Цель сохранена.";
        return RedirectToPage(new { ClientId, Days, Until = Until?.ToString("yyyy-MM-dd") });
    }

    public async Task<IActionResult> OnPostCheckInAsync()
    {
        if (!await LoadAsync()) return NotFound();
        if (IsCoachView) return Forbid();
        // Only this form's fields are relevant; goal validation belongs to OnPostGoal.
        foreach (var key in ModelState.Keys.Where(k => k.StartsWith("Goal.")).ToArray()) ModelState.Remove(key);
        if (!ModelState.IsValid) return Page();
        var entry = await db.DailyCheckIns.FindAsync(currentUser.Id, Data.Today);
        if (entry is null) { entry = new DailyCheckIn { UserId = currentUser.Id, Date = Data.Today }; db.DailyCheckIns.Add(entry); }
        entry.WaterMl = CheckIn.WaterMl; entry.Steps = CheckIn.Steps; entry.SleepMinutes = CheckIn.SleepMinutes;
        entry.ReadingMinutes = CheckIn.ReadingMinutes;
        entry.NutritionLogged = CheckIn.NutritionLogged; entry.StretchingDone = CheckIn.StretchingDone;
        await db.SaveChangesAsync();
        StatusMessage = "Показатели на сегодня сохранены.";
        return RedirectToPage(new { Days, Until = Until?.ToString("yyyy-MM-dd") });
    }

    public async Task<IActionResult> OnPostLayoutAsync([FromBody] DashboardLayoutInput? input)
    {
        var subject = await dashboard.GetSubjectAsync(ClientId);
        if (subject is null) return NotFound();
        if (!ModelState.IsValid || !DashboardWidgets.IsValid(input?.Widgets))
            return BadRequest(new { error = "Не удалось прочитать раскладку. Обновите страницу и попробуйте ещё раз." });

        // Layout belongs to the viewer, never to the client whose statistics are open.
        var mode = subject.Id == currentUser.Id ? "personal" : "coach";
        var preference = await db.DashboardLayoutPreferences.FindAsync(currentUser.Id, mode);
        if (preference is null)
        {
            preference = new DashboardLayoutPreference { UserId = currentUser.Id, Mode = mode };
            db.DashboardLayoutPreferences.Add(preference);
        }
        var widgets = DashboardWidgets.PinWelcomeFirst(input!.Widgets!);
        preference.WidgetsJson = DashboardWidgets.Serialize(widgets);
        await db.SaveChangesAsync();
        Response.Headers.CacheControl = "no-cache, no-store";
        return new JsonResult(new { widgets });
    }

    private async Task<bool> LoadAsync()
    {
        var subject = await dashboard.GetSubjectAsync(ClientId);
        if (subject is null) return false;
        Days = Days is 7 or 30 or 365 ? Days : 7;
        WeekOffset = Math.Clamp(WeekOffset, -52, 52);
        ChartWeeks = ChartWeeks is 2 or 4 or 6 ? ChartWeeks : 6;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        Until = Until is null || Until > today || Until < today.AddYears(-20) ? today : Until;
        Data = await dashboard.LoadAsync(subject, Days, Until.Value);
        if (!IsCoachView)
        {
            ActiveWorkout = await workouts.ActiveAsync();
            Templates = await workouts.TemplatesAsync();
        }
        Clients = await dashboard.GetClientsAsync();
        if (CurrentPage == "/Dashboard")
        {
            if (!IsCoachView && await features.CanUseAsync(Feature.CoachClientAlerts)) Attention = await attention.LoadAsync();
            if (!IsCoachView)
            {
                ActiveProgram = (await programs.ListAsync("mine")).FirstOrDefault(p => p.StartDate.HasValue && p.StartDate.Value.AddDays(p.Weeks * 7) > today);
                if (ActiveProgram is not null) NextProgramWorkout = ProgramSchedule.Next(ActiveProgram, await programs.SessionsAsync(ActiveProgram));
                CanAnalyzePrograms = await features.CanUseAsync(Feature.ProgressionRecommendations);
                if (ActiveProgram is { } activeProgram)
                {
                    NextGoalExercise = (NextProgramWorkout?.Workout ?? activeProgram.Workouts.OrderBy(w => w.Order).FirstOrDefault())?
                        .Exercises.OrderBy(e => e.Order).FirstOrDefault();
                    if (CanAnalyzePrograms)
                    {
                        var exerciseIds = await db.TrainingRecommendations.AsNoTracking()
                            .Where(r => r.TrainingProgramId == activeProgram.Id && r.UserId == Data.Subject.Id &&
                                r.Status == RecommendationStatus.New && r.ProgramRevision == activeProgram.Revision)
                            .OrderByDescending(r => r.CreatedAtUtc).Select(r => r.ProgramWorkoutExerciseId).ToListAsync();
                        foreach (var exerciseId in exerciseIds.Distinct())
                        {
                            // Reuse the existing evidence/expiry checks without generating or applying on GET.
                            var proposal = (await recommendations.ListAsync(activeProgram.Id, exerciseId))
                                .FirstOrDefault(r => r.Status == RecommendationStatus.New);
                            if (proposal is null) continue;
                            NextRecommendation = proposal;
                            NextGoalExercise = activeProgram.Workouts.SelectMany(w => w.Exercises).Single(e => e.Id == exerciseId);
                            break;
                        }
                    }
                }
            }
        }
        var preference = await db.DashboardLayoutPreferences.FindAsync(currentUser.Id, IsCoachView ? "coach" : "personal");
        UsesDefaultLayout = preference is null;
        Widgets = DashboardWidgets.Read(preference?.WidgetsJson, IsCoachView);
        ViewData["Dashboard"] = this;
        Response.Headers.CacheControl = "no-cache, no-store";
        return true;
    }
}

public sealed class DashboardLayoutInput
{
    public List<DashboardWidgetPlacement>? Widgets { get; set; }
}

public sealed class GoalInput
{
    [Required(ErrorMessage = "Напишите цель."), StringLength(120, ErrorMessage = "Не больше 120 символов.")]
    public string Title { get; set; } = "Тренироваться регулярно";
    [Range(1, 14, ErrorMessage = "Укажите от 1 до 14 тренировок в неделю.")]
    public int WeeklyWorkouts { get; set; } = 4;
    [StringLength(1000, ErrorMessage = "Не больше 1000 символов.")] public string? TrainerNote { get; set; }
}

public sealed class CheckInInput
{
    [Range(0, 15000, ErrorMessage = "Вода: от 0 до 15 000 мл.")] public int WaterMl { get; set; }
    [Range(0, 100000, ErrorMessage = "Шаги: от 0 до 100 000.")] public int Steps { get; set; }
    [Range(0, 1440, ErrorMessage = "Сон: от 0 до 1 440 минут.")] public int SleepMinutes { get; set; }
    [Range(0, 1440, ErrorMessage = "Чтение: от 0 до 1 440 минут.")] public int ReadingMinutes { get; set; }
    public bool NutritionLogged { get; set; }
    public bool StretchingDone { get; set; }
}
