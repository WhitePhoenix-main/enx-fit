using enx_fit.Models;
using enx_fit.Pages.Dashboard;
using enx_fit.Presentation.Exercises;
using enx_fit.Services.Dashboard;
using enx_fit.Services.Programs;

namespace enx_fit.ViewModels.Dashboard;

// Presentation for the athlete's home; reference values never participate in commands.
public sealed class AthleteHomeViewModel
{
    public SubscriptionDashboardViewModel Subscription { get; }
    public DashboardModel Page => Subscription.Page;
    public bool Preview => Subscription.Preview;
    public DateOnly Today => Preview ? new(2026, 10, 20) : Page.Data.Today;
    public WorkoutSession? Active { get; }
    public WorkoutSession? Session { get; }
    public ProgramOccurrence? ProgramWorkout { get; }
    public WorkoutSession? Last { get; }
    public HomeNextWorkout? Next { get; }
    public string State { get; }
    public string Title { get; }
    public string Eyebrow => State switch
    {
        "active" => Active?.State == WorkoutStatus.Paused ? "Тренировка на паузе" : "Тренировка идёт",
        "completed" => "Сегодня выполнено",
        "rest" => "Время восстановиться",
        "empty" => "Начни в своём темпе",
        _ => "Сегодня по плану"
    };
    public IReadOnlyList<string> Exercises { get; }
    public int? Minutes { get; }
    public int CompletedSets => Active is null ? 0 : DashboardData.WorkingSets(Active).Count();
    public int WeeklyDone => Preview ? 1 : Page.Data.CountCompleted(Week[0].Date, Today);
    public int WeeklyGoal => Preview ? 5 : Page.Data.Settings.WeeklyWorkoutGoal;
    public IReadOnlyList<DashboardWeekDay> Week { get; }
    public IReadOnlyList<ProgramOccurrence> Occurrences { get; }
    public int MonthlyCount { get; }
    public int MonthlyMinutes { get; }
    public int MonthlyRecords { get; }
    public string ProgramName => Preview ? "Сила и масса" : Page.ActiveProgram?.Name ?? "Выбрать программу";
    public int ProgramWeek => Preview ? 3 : Page.ActiveProgram?.StartDate is { } start
        ? Math.Clamp((Today.DayNumber - start.DayNumber) / 7 + 1, 1, Page.ActiveProgram.Weeks) : 0;
    public int ProgramWeeks => Preview ? 8 : Page.ActiveProgram?.Weeks ?? 0;

    public AthleteHomeViewModel(SubscriptionDashboardViewModel subscription)
    {
        Subscription = subscription;
        var data = Page.Data;
        Active = Preview ? null : Page.ActiveWorkout;
        Session = Preview ? null : Active ?? data.Workouts
            .Where(w => w.Date == Today && w.State == WorkoutStatus.Planned).OrderBy(w => w.Id).FirstOrDefault();
        ProgramWorkout = !Preview && Page.NextProgramWorkout?.Date == Today ? Page.NextProgramWorkout : null;
        if (Session is null && ProgramWorkout is null && !Preview) Session = Page.CompletedToday;
        Occurrences = Page.ActiveProgram is { } program ? ProgramSchedule.Occurrences(program).ToList() : [];
        var plannedNext = data.Workouts.Where(w => w.Date > Today && w.State == WorkoutStatus.Planned)
            .OrderBy(w => w.Date).ThenBy(w => w.Id).FirstOrDefault();
        var programNext = Occurrences.FirstOrDefault(o => o.Date > Today &&
            !data.Workouts.Any(w => ProgramSchedule.Matches(w, o) && w.State == WorkoutStatus.Completed));
        Next = Preview ? new("Кардио", Today.AddDays(1), 4, EstimatedMinutes: 30)
            : plannedNext is not null && (programNext is null || plannedNext.Date <= programNext.Date)
                ? new(plannedNext.Title ?? "Тренировка", plannedNext.Date, plannedNext.WorkoutExercises.Count,
                    WorkoutId: plannedNext.Id, EstimatedMinutes: plannedNext.PlannedMinutes)
                : programNext is not null ? new(programNext.Workout.Name, programNext.Date, programNext.Workout.Exercises.Count,
                    ProgramId: Page.ActiveProgram!.Id, EstimatedMinutes: programNext.Workout.EstimatedMinutes) : null;
        State = Preview ? "planned" : Active is not null ? "active" : Session?.State == WorkoutStatus.Completed ? "completed"
            : Session is not null || ProgramWorkout is not null ? "planned" : Next is not null ? "rest" : "empty";
        Title = Preview ? "Ноги" : Session?.Title ?? ProgramWorkout?.Workout.Name ?? (State == "rest" ? "День отдыха" : "Начни с плана");
        Exercises = Preview ? ["Приседания", "Жим ногами", "Румынская тяга", "Сгибания ног", "Выпады", "Подъёмы на носки"]
            : Session is not null ? Session.WorkoutExercises.OrderBy(e => e.Order).Select(e => ExercisePresentation.DisplayName(e.Exercise)).ToList()
            : ProgramWorkout?.Workout.Exercises.OrderBy(e => e.Order).Select(e => ExercisePresentation.DisplayName(e.Exercise)).ToList() ?? [];
        Minutes = Preview ? 50 : Session?.State == WorkoutStatus.Completed ? Session.DurationSeconds / 60
            : Session?.PlannedMinutes ?? ProgramWorkout?.Workout.EstimatedMinutes;
        Last = Preview ? null : data.Completed.OrderByDescending(w => w.Date).ThenByDescending(w => w.Id).FirstOrDefault();
        var monday = Today.AddDays(-((int)Today.DayOfWeek + 6) % 7);
        string[] titles = ["Верх", "Ноги", "Кардио", "Отдых", "Верх", "Ноги", "Отдых"];
        Week = Enumerable.Range(0, 7).Select(i =>
        {
            var date = monday.AddDays(i);
            var workout = data.Workouts.Where(w => w.Date == date && w.State != WorkoutStatus.Cancelled)
                .OrderBy(w => w.State == WorkoutStatus.Completed ? 0 : 1).ThenBy(w => w.Id).FirstOrDefault();
            var occurrence = Occurrences.FirstOrDefault(o => o.Date == date);
            var done = Preview ? i == 0 : data.CountCompleted(date, date) > 0;
            var planned = Preview ? titles[i] != "Отдых" : workout is not null || occurrence is not null;
            return new DashboardWeekDay(date, new[] { "Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Вс" }[i],
                Preview ? titles[i] : workout?.Title ?? occurrence?.Workout.Name ?? (done ? "Выполнено" : "Отдых"), done, planned, date == Today);
        }).ToList();
        var recent = data.Completed.Where(w => w.Date >= Today.AddDays(-27) && w.Date <= Today).ToList();
        MonthlyCount = Preview ? 12 : data.CountCompleted(Today.AddDays(-27), Today);
        MonthlyMinutes = Preview ? 560 : recent.Sum(w => w.DurationSeconds ?? 0) / 60;
        MonthlyRecords = Preview ? 3 : data.NewRecords;
    }

    public IEnumerable<WorkoutSession> SessionsOn(DateOnly date) => Page.Data.Workouts
        .Where(w => w.Date == date && w.State != WorkoutStatus.Cancelled).OrderBy(w => w.Id);

    public IEnumerable<ProgramOccurrence> PlansOn(DateOnly date) => Occurrences.Where(o => o.Date == date &&
        !Page.Data.Workouts.Any(w => w.State != WorkoutStatus.Cancelled && ProgramSchedule.Matches(w, o)));

    public static string ShortTitle(string title) => title.Contains("верх", StringComparison.OrdinalIgnoreCase) ? "Верх"
        : title.Contains("ног", StringComparison.OrdinalIgnoreCase) ? "Ноги"
        : title.Contains("кардио", StringComparison.OrdinalIgnoreCase) ? "Кардио"
        : title.Contains("всё тело", StringComparison.OrdinalIgnoreCase) ? "Всё тело" : title;

    public static string Noun(int count, string one, string few, string many) => count % 100 is >= 11 and <= 14
        ? many : count % 10 == 1 ? one : count % 10 is >= 2 and <= 4 ? few : many;
}
