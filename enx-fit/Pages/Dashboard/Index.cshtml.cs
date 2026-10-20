using System.ComponentModel.DataAnnotations;
using enx_fit.Areas.Identity.Data;
using enx_fit.Data;
using enx_fit.Diagnostics;
using enx_fit.ModelBinding;
using enx_fit.Models;
using enx_fit.Presentation.Dashboard;
using enx_fit.Security;
using enx_fit.Services.Coaching;
using enx_fit.Services.Dashboard;
using enx_fit.Services.Programs;
using enx_fit.Services.Workouts;
using enx_fit.ViewModels.Dashboard;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace enx_fit.Pages.Dashboard;

[MinimumRole(UserRole.User)]
[RequestSizeLimit(16384)]
public class DashboardModel(DashboardService dashboard, ApplicationDbContext db, CurrentUser currentUser,
    IFeatureAccessService features, CoachAttentionService attention, TrainingProgramService programs, WorkoutService workouts,
    IRecommendationService recommendations, RequestPerformance performance, DashboardQuery homeQuery, DashboardCommands commands) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? ClientId { get; set; }
    [BindProperty(SupportsGet = true)] public int Days { get; set; } = 7;
    [BindProperty(SupportsGet = true)] public DateOnly? Until { get; set; }
    [BindProperty(SupportsGet = true)] public int WeekOffset { get; set; }
    [BindProperty(SupportsGet = true)] public int ChartWeeks { get; set; } = 6;
    [BindProperty(SupportsGet = true)] public int ProgressWeeks { get; set; } = 6;
    [BindProperty(SupportsGet = true)] [ModelBinder(BinderType = typeof(ExerciseIdModelBinder))] public Guid? ProgressExercise { get; set; }
    [BindProperty(SupportsGet = true)] public string? ProgressTab { get; set; } = "overview";
    [BindProperty(SupportsGet = true)] public string? ProgressMetric { get; set; } = "consistency";
    // An explicit visual snapshot reproduces the reference content without altering user records.
    [BindProperty(SupportsGet = true)] public bool Reference { get; set; }
    [BindProperty(SupportsGet = true), StringLength(10)] public string? ReferencePlan { get; set; }
    public SubscriptionDashboardViewModel SubscriptionHome { get; private set; } = null!;
    [BindProperty(SupportsGet = true)] public DateOnly? Month { get; set; }
    [BindProperty(SupportsGet = true), StringLength(120)] public string? Search { get; set; }
    [BindProperty(SupportsGet = true)] public string? WorkoutProgram { get; set; }
    [BindProperty(SupportsGet = true)] public int? SelectedWorkout { get; set; }
    [BindProperty(SupportsGet = true)] public DateOnly? WorkoutDate { get; set; }
    public IReadOnlyList<TrainingProgram> WorkoutPrograms { get; private set; } = [];
    public IReadOnlyList<enx_fit.ViewModels.Workouts.WorkoutTemplateCard> CoachTemplates { get; private set; } = [];
    public IReadOnlyList<CoachCalendarEntry> CoachCalendar { get; private set; } = [];
    [BindProperty] public GoalInput Goal { get; set; } = new();
    [BindProperty] public CheckInInput CheckIn { get; set; } = new();
    public DashboardData Data { get; private set; } = null!;
    public IReadOnlyList<ClientSummary> Clients { get; private set; } = [];
    public CoachAttention? Attention { get; private set; }
    public TrainingProgram? ActiveProgram { get; private set; }
    public ProgramOccurrence? NextProgramWorkout { get; private set; }
    public bool CanAnalyzePrograms { get; private set; }
    public bool CanManageClients { get; private set; }
    public TrainingRecommendation? NextRecommendation { get; private set; }
    public ProgramWorkoutExercise? NextGoalExercise { get; private set; }
    public WorkoutSession? ActiveWorkout { get; private set; }
    public WorkoutSession? CompletedToday => Data.Completed.FirstOrDefault(w => w.Date == Data.Today);
    public HomeNextWorkout? UpcomingWorkout
    {
        get
        {
            if (Reference || IsCoachView || ActiveWorkout is not null ||
                Data.NextWorkout?.Date == Data.Today || NextProgramWorkout?.Date == Data.Today) return null;
            var planned = Data.NextWorkout is { } w && w.Date > Data.Today
                ? new HomeNextWorkout(w.Title ?? "Запланированная тренировка", w.Date, w.WorkoutExercises.Count, WorkoutId: w.Id) : null;
            var program = NextProgramWorkout is { } next && next.Date > Data.Today && ActiveProgram is { } p
                ? new HomeNextWorkout(next.Workout.Name, next.Date, next.Workout.Exercises.Count,
                    ProgramId: p.Id, EstimatedMinutes: next.Workout.EstimatedMinutes) : null;
            return planned is not null && (program is null || planned.Date <= program.Date) ? planned : program;
        }
    }
    private HomeProgressViewModel? homeProgress;
    public HomeProgressViewModel HomeProgress => homeProgress ??= new(this);
    public bool ShowOnboarding => !IsCoachView && ActiveWorkout is null && Data.CompletedCount == 0 && Data.Measurements.Count == 0;
    public bool ShowSetupPrompt => !Reference && ShowOnboarding && Data.Settings.SetupStatus == SetupStatus.NotStarted;
    public bool UsesDefaultLayout { get; private set; }

    public async Task<IActionResult> OnPostStartAsync(string source, int? templateId, int? plannedId, int? programId, int? previousId)
    {
        var subject = await dashboard.GetSubjectAsync(ClientId);
        if (subject is null) return NotFound();
        if (subject.Id != currentUser.Id) return Forbid();
        try
        {
            var id = source == "program" && programId.HasValue
                ? await programs.StartNextAsync(programId.Value)
                : await workouts.StartAsync(source, templateId, plannedId, previousId);
            return RedirectToPage("/Training/Workouts/Details", new { id });
        }
        catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); }
        catch (ProgramOperationException ex) { ModelState.AddModelError("", ex.Message); }
        await LoadAsync(subject);
        return Page();
    }
    public List<DashboardWidgetPlacement> Widgets { get; private set; } = [];
    public string CurrentPage => PageContext.ActionDescriptor.ViewEnginePath;
    public DashboardSection? Section => DashboardSections.Find(CurrentPage);
    public object NavigationValues => new { ClientId = IsCoachView ? Data.Subject.Id : null, Days, Until = Until?.ToString("yyyy-MM-dd"), Reference = Reference ? (bool?)true : null };
    public bool IsCoachView => Data.Subject.Id != currentUser.Id;
    public bool IsTrainer => currentUser.Role == UserRole.Trainer || currentUser.IsAdministrator;
    [TempData] public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await LoadAsync()) return NotFound();
        Goal = new() { Title = Data.Settings.GoalTitle, WeeklyWorkouts = Data.Settings.WeeklyWorkoutGoal, TrainerNote = Data.Settings.TrainerNote };
        var today = Data.TodayCheckIn;
        CheckIn = new() { WaterMl = today.WaterMl, Steps = today.Steps, SleepMinutes = today.SleepMinutes, ReadingMinutes = today.ReadingMinutes, NutritionLogged = today.NutritionLogged, StretchingDone = today.StretchingDone };
        return Page();
    }

    public async Task<PartialViewResult> OnGetTemplatesAsync()
    {
        Response.Headers.CacheControl = "no-cache, no-store";
        var templates = await performance.MeasureAsync("templates", workouts.TemplatesAsync);
        return Partial("/Pages/Shared/Workouts/_TemplateOptions.cshtml", templates);
    }

    public async Task<IActionResult> OnPostGoalAsync()
    {
        var subject = await dashboard.GetSubjectAsync(ClientId);
        if (subject is null) return NotFound();
        if (!ModelState.IsValid) { await LoadAsync(subject); return Page(); }
        await commands.SaveGoalAsync(subject.Id, Goal);
        NormalizePeriod(currentUser.LocalToday);
        StatusMessage = "Цель сохранена.";
        return RedirectToPage(new { ClientId, Days, Until = Until?.ToString("yyyy-MM-dd") });
    }

    public async Task<IActionResult> OnPostCheckInAsync()
    {
        var subject = await dashboard.GetSubjectAsync(ClientId);
        if (subject is null) return NotFound();
        if (subject.Id != currentUser.Id) return Forbid();
        // Only this form's fields are relevant; goal validation belongs to OnPostGoal.
        foreach (var key in ModelState.Keys.Where(k => k.StartsWith("Goal.")).ToArray()) ModelState.Remove(key);
        if (!ModelState.IsValid) { await LoadAsync(subject); return Page(); }
        await commands.SaveCheckInAsync(CheckIn);
        NormalizePeriod(currentUser.LocalToday);
        StatusMessage = "Показатели на сегодня сохранены.";
        return RedirectToPage(new { Days, Until = Until?.ToString("yyyy-MM-dd") });
    }

    public async Task<IActionResult> OnPostWellbeingAsync(string choice, int? feeling)
    {
        var subject = await dashboard.GetSubjectAsync(ClientId);
        if (subject is null) return NotFound();
        if (Reference || subject.Id != currentUser.Id) return Forbid();
        foreach (var key in ModelState.Keys.Where(k => k.StartsWith("Goal.") || k.StartsWith("CheckIn.")).ToArray()) ModelState.Remove(key);
        if (choice is not ("feeling" or "clear" or "rest" or "resume") || choice == "feeling" && feeling is not (>= 1 and <= 3))
            ModelState.AddModelError("feeling", "Выберите самочувствие или день отдыха.");
        if (!ModelState.IsValid) { await LoadAsync(subject); return Page(); }
        await commands.SaveWellbeingAsync(choice, feeling);
        StatusMessage = choice switch { "rest" => "Сегодня выбран отдых. План тренировок сохранён.", "resume" => "Отметка отдыха убрана.", "clear" => "Отметка самочувствия убрана.", _ => "Самочувствие на сегодня сохранено." };
        return RedirectToPage(new { ClientId, Month = Month?.ToString("yyyy-MM-dd") });
    }

    public async Task<IActionResult> OnPostLayoutAsync([FromBody] DashboardLayoutInput? input)
    {
        var subject = await performance.MeasureAsync("subject", () => dashboard.GetSubjectAsync(ClientId));
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

    private void NormalizePeriod(DateOnly latestDate)
    {
        Days = Days is 7 or 30 or 365 ? Days : 7;
        var today = currentUser.LocalToday;
        Until = Until is null || Until > latestDate || Until < today.AddYears(-20) ? latestDate : Until;
    }

    private async Task<bool> LoadAsync(ApplicationUser? authorizedSubject = null)
    {
        if (CurrentPage == "/Training/Index" && ClientId is null && await features.CanUseAsync(Feature.CoachClientManagement))
            ClientId = (await dashboard.GetClientsAsync()).FirstOrDefault()?.Id;
        var subject = authorizedSubject ?? await performance.MeasureAsync("subject", () => dashboard.GetSubjectAsync(ClientId));
        if (subject is null) return false;
        if (CurrentPage == "/Training/Index" && !Request.Query.ContainsKey("Days")) Days = 30;
        WeekOffset = Math.Clamp(WeekOffset, -52, 52);
        ProgressWeeks = ProgressWeeks is 2 or 4 or 6 or 12 ? ProgressWeeks : 6;
        ChartWeeks = ChartWeeks is 2 or 4 or 6 or 12 ? ChartWeeks : 6;
        if (CurrentPage == "/Dashboard/Index" && !Request.Query.ContainsKey("ChartWeeks")) ChartWeeks = 4;
        if (!Reference && CurrentPage == "/Progress/Index" && !Request.Query.ContainsKey("ChartWeeks")) ChartWeeks = ProgressWeeks;
        ProgressTab = ProgressTab is "overview" or "analytics" or "records" ? ProgressTab : "overview";
        ProgressMetric = ProgressMetric is "consistency" or "strength" or "body" || (ProgressTab == "analytics" && ProgressMetric == "volume") ? ProgressMetric : "consistency";
        var today = currentUser.LocalToday;
        var latestDate = Reference && CurrentPage == "/Progress/Index" ? new DateOnly(2026, 10, 2) : today;
        NormalizePeriod(latestDate);
        DashboardReadModel? home = null;
        if (CurrentPage == "/Dashboard/Index")
        {
            home = await performance.MeasureAsync("history", () => homeQuery.LoadAsync(subject, Days, Until!.Value, WeekOffset, ChartWeeks));
            Data = home.Data;
        }
        else Data = await performance.MeasureAsync("history", () => dashboard.LoadAsync(subject, Days, Until!.Value));
        homeProgress = null;
        if (CurrentPage is "/Training/Index" or "/Training/Plans")
        {
            Search = Search?.Trim();
            if (Search?.Length > 120) Search = Search[..120];
            if (WorkoutProgram != "standalone" && !int.TryParse(WorkoutProgram, out _)) WorkoutProgram = null;
            var calendarDate = Month ?? (Reference ? new DateOnly(2026, 10, 2) : Until!.Value);
            if (calendarDate.Year < today.Year - 20 || calendarDate.Year > today.Year + 5) calendarDate = today;
            Month = new(calendarDate.Year, calendarDate.Month, 1);
            WorkoutPrograms = await db.TrainingPrograms.AsNoTracking().AsSplitQuery()
                .Where(p => p.OwnerId == subject.Id && !p.IsTemplate)
                .Include(p => p.ScheduleChanges)
                .Include(p => p.Workouts).ThenInclude(w => w.Exercises).ThenInclude(e => e.Exercise)
                .OrderBy(p => p.Name).ToListAsync();
            ActiveProgram = WorkoutPrograms.FirstOrDefault(p => !p.IsArchived && p.StartDate.HasValue && p.StartDate.Value.AddDays(p.Weeks * 7) > today);
            if (ActiveProgram is not null) NextProgramWorkout = ProgramSchedule.Next(ActiveProgram, Data.Workouts.Where(w => w.TrainingProgramId == ActiveProgram.Id).ToList(), today);
        }
        if (!IsCoachView)
        {
            ActiveWorkout = Data.Workouts.Where(workout => workout.IsActive)
                .OrderByDescending(workout => workout.CreatedAtUtc).FirstOrDefault();
        }
        Clients = await performance.MeasureAsync("clients", () => dashboard.GetClientsAsync());
        CanManageClients = await features.CanUseAsync(Feature.CoachClientManagement);
        if (CurrentPage == "/Training/Index")
        {
            CoachTemplates = await HttpContext.RequestServices.GetRequiredService<WorkoutTemplateCatalog>().ListAsync();
            var monday = today.AddDays(-((int)today.DayOfWeek + 6) % 7);
            if (CanManageClients) CoachCalendar = await HttpContext.RequestServices.GetRequiredService<CoachCalendarService>().WeekAsync(monday);
        }
        if (CurrentPage is "/Dashboard/Index" or "/Progress/Index")
        {
            if (CurrentPage == "/Dashboard/Index" && !IsCoachView && await features.CanUseAsync(Feature.CoachClientAlerts))
                Attention = await performance.MeasureAsync("alerts", () => attention.LoadAsync(assignedOnly: true));
            if (!IsCoachView)
            {
                var candidates = (await performance.MeasureAsync("programs", () => programs.DashboardProgramsAsync(today)))
                    .Select(p => new { Program = p, Next = ProgramSchedule.Next(p, Data.Workouts.Where(w => w.TrainingProgramId == p.Id).ToList(), today) }).ToList();
                var selectedProgram = candidates.FirstOrDefault(p => p.Program.Id == ActiveWorkout?.TrainingProgramId)
                    ?? candidates.Where(p => p.Next is not null).OrderBy(p => p.Next!.Date).ThenBy(p => p.Program.Id).FirstOrDefault()
                    ?? candidates.FirstOrDefault();
                ActiveProgram = selectedProgram?.Program;
                NextProgramWorkout = selectedProgram?.Next;
                CanAnalyzePrograms = await features.CanUseAsync(Feature.ProgressionRecommendations);
                if (ActiveProgram is { } activeProgram)
                {
                    NextGoalExercise = (NextProgramWorkout?.Workout ?? activeProgram.Workouts.OrderBy(w => w.Order).FirstOrDefault())?
                        .Exercises.OrderBy(e => e.Order).FirstOrDefault();
                    if (CanAnalyzePrograms)
                    {
                        using var measurement = performance.Measure("recommendations");
                        NextRecommendation = await recommendations.NextForDashboardAsync(activeProgram);
                        if (NextRecommendation is { } proposal)
                        {
                            NextGoalExercise = activeProgram.Workouts.SelectMany(w => w.Exercises)
                                .Single(e => e.Id == proposal.ProgramWorkoutExerciseId);
                        }
                    }
                }
            }
        }
        if (home is not null)
        {
            UsesDefaultLayout = home.UsesDefaultLayout;
            Widgets = DashboardWidgets.Read(home.WidgetsJson, IsCoachView);
        }
        else
        {
            var preference = await performance.MeasureAsync("layout", async () =>
                await db.DashboardLayoutPreferences.FindAsync(currentUser.Id, IsCoachView ? "coach" : "personal"));
            UsesDefaultLayout = preference is null;
            Widgets = DashboardWidgets.Read(preference?.WidgetsJson, IsCoachView);
        }
        ViewData["Dashboard"] = this;
        if (CurrentPage == "/Dashboard/Index")
        {
            var access = await features.GetAsync();
            var coachHome = !IsCoachView && access.Plan == SubscriptionPlan.Coach && access.CanUse(Feature.CoachClientManagement);
            var query = HttpContext.RequestServices.GetRequiredService<SubscriptionDashboardQuery>();
            var cards = Reference ? [] : await query.ProgramsAsync(coachHome);
            var overview = !Reference && coachHome ? await query.CoachAsync(today) : null;
            SubscriptionHome = new(this, access, cards, overview);
        }
        Response.Headers.CacheControl = "no-cache, no-store";
        return true;
    }
}
