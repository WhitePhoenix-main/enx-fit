using enx_fit.Models;
using enx_fit.Pages.Dashboard;
using enx_fit.Security;
using enx_fit.Services.Dashboard;
using enx_fit.Services.Programs;

namespace enx_fit.ViewModels.Dashboard;

public sealed record DashboardWeekDay(DateOnly Date, string Name, string Title, bool Done, bool Planned, bool Today);
public sealed record DashboardBar(string Label, decimal Value);

public sealed class SubscriptionDashboardViewModel
{
    public DashboardModel Page { get; }
    public SubscriptionPlan Plan { get; }
    public string Theme => Plan.ToString().ToLowerInvariant();
    public string Badge => Plan.ToString();
    public bool Preview => Page.Reference;
    public bool Coach { get; }
    public bool Premium => Plan != SubscriptionPlan.Free;
    public bool CanRecommend { get; }
    public string Name => Preview ? "Алексей" : Page.Data.Name;
    public DateOnly Today => Preview ? new(2024, 10, 11) : Page.Data.Today;
    public int WeeklyGoal => Preview ? 4 : Math.Clamp(Page.Data.Settings.WeeklyWorkoutGoal, 1, 7);
    public int WeeklyDone => Preview ? 3 : Page.Data.CountCompleted(Today.AddDays(-((int)Today.DayOfWeek + 6) % 7), Today);
    public int Streak => Preview ? Premium ? 12 : 3 : Page.Data.Streak;
    public int BestStreak { get; }
    public int ActiveClients => Preview ? 24 : Page.Attention?.ActiveClients ?? Page.Clients.Count;
    public int ScheduledToday => Preview ? 12 : Math.Max(Page.Attention?.ScheduledToday ?? 0, Overview.ScheduledTotal);
    public int CompletedClients => Preview ? 16 : Overview.TrainedToday;
    public int AttentionCount => Preview ? 4 : Page.Attention?.Alerts.Select(a => a.ClientId).Distinct().Count() ?? 0;
    public int InactiveClients => Preview ? 2 : Page.Clients.Count(c => c.LastWorkout is null || c.LastWorkout < Today.AddDays(-7));
    public IReadOnlyList<DashboardProgramCard> Programs { get; }
    public CoachOverview Overview { get; }
    public IReadOnlyList<DashboardWeekDay> Week { get; }
    public IReadOnlyList<DashboardBar> Bars { get; }
    public WorkoutSession? Session => Page.ActiveWorkout ?? Page.Data.NextWorkout;
    public bool HasWorkout => Preview || Session is not null || Page.NextProgramWorkout is not null;
    public string WorkoutTitle => Preview ? "Верх тела" : Session?.Title ?? Page.NextProgramWorkout?.Workout.Name ?? "Ваша следующая тренировка";
    public int ExerciseCount => Preview ? 4 : Session?.WorkoutExercises.Count ?? Page.NextProgramWorkout?.Workout.Exercises.Count ?? 0;
    public int? Minutes => Preview ? Premium ? 58 : 52 : Session?.DurationSeconds is > 0 ? Session.DurationSeconds / 60 : Page.NextProgramWorkout?.Workout.EstimatedMinutes;
    public string WorkoutDescription => Preview
        ? Premium ? "Выбрана на основе твоего прогресса, восстановления и целей. Сегодня акцент на рост силы и объёма верхней части тела." : "Базовая тренировка на силу и тонус верхней части тела."
        : Page.ActiveWorkout is not null ? "Всё готово, чтобы продолжить. Ваши подходы и результаты сохранены."
        : HasWorkout ? "Тренировка из вашего плана. Откройте состав упражнений и двигайтесь к своей цели."
        : "Выберите готовую программу или начните своё занятие. Каждый шаг делает вас сильнее.";
    public SubscriptionDashboardViewModel(DashboardModel page, FeatureAccess access, IReadOnlyList<DashboardProgramCard> programs, CoachOverview? overview)
    {
        Page = page;
        Plan = page.Reference && Enum.TryParse<SubscriptionPlan>(page.ReferencePlan, true, out var plan) && Enum.IsDefined(plan) ? plan : access.Plan;
        Coach = !page.IsCoachView && (Preview ? Plan == SubscriptionPlan.Coach : Plan == SubscriptionPlan.Coach && access.CanUse(Feature.CoachClientManagement));
        CanRecommend = access.CanUse(Feature.ProgressionRecommendations);
        Programs = Preview ? PreviewPrograms(Coach, Premium) : programs;
        Overview = overview ?? new([], [], []);
        var best = 0; var run = 0; DateOnly? previous = null;
        foreach (var day in page.Data.CompletedDates.Distinct().Order())
        { run = previous?.AddDays(1) == day ? run + 1 : 1; best = Math.Max(best, run); previous = day; }
        BestStreak = Preview ? Premium ? 21 : 7 : best;
        var monday = Today.AddDays(-((int)Today.DayOfWeek + 6) % 7);
        var occurrences = page.ActiveProgram is { } active ? ProgramSchedule.Occurrences(active).ToList() : [];
        string[] names = ["Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Вс"];
        string[] sample = Premium ? ["Верх тела", "Ноги", "Отдых", "Верх тела", "Руки", "Кардио", "Отдых"] : ["Верх тела", "Отдых", "Ноги", "Отдых", "Верх тела", "Отдых", "Кардио"];
        Week = Enumerable.Range(0, 7).Select(i =>
        {
            var date = monday.AddDays(i);
            var workout = page.Data.Workouts.FirstOrDefault(w => w.Date == date && w.State is WorkoutStatus.Completed or WorkoutStatus.Planned);
            var occurrence = occurrences.FirstOrDefault(o => o.Date == date);
            return new DashboardWeekDay(date, names[i], Preview ? sample[i] : workout?.Title ?? occurrence?.Workout.Name ?? "Отдых",
                Preview ? i < (Premium ? 2 : 3) : page.Data.CompletedDates.Contains(date),
                Preview ? sample[i] != "Отдых" : workout is not null || occurrence is not null, date == Today);
        }).ToList();
        var history = Coach ? Overview.Days : page.Data.History?.Days ?? page.Data.Completed.GroupBy(w => w.Date).Select(g => new DashboardDay(g.Key, g.Count(), g.Sum(DashboardData.Volume))).ToList();
        var chartStart = monday.AddDays(-35);
        Bars = Enumerable.Range(0, 6).Select(i =>
        {
            var from = chartStart.AddDays(i * 7); var to = from.AddDays(7);
            var value = Preview ? Coach ? new decimal[] { 9, 12, 16, 17, 21, 24 }[i] : Premium ? new decimal[] { 3600, 4700, 6200, 7300, 9000, 11200 }[i] : new decimal[] { 1.6m, 2.4m, 3.3m, 4, 4.5m, 5.6m }[i]
                : Premium && !Coach ? history.Where(d => d.Date >= from && d.Date < to).Sum(d => d.Volume) : history.Where(d => d.Date >= from && d.Date < to).Sum(d => d.Count);
            return new DashboardBar(Preview ? new[] { "2 сен", "9 сен", "16 сен", "23 сен", "30 сен", "6 окт" }[i] : from.ToString("d MMM", DashboardWorkoutsViewModel.Russian), value);
        }).ToList();
    }
    private static DashboardProgramCard[] PreviewPrograms(bool coach, bool premium) => coach
        ? [new(0,"Силовое тело",12,3,ProgramLevel.Intermediate,false,"Шаблон"),new(0,"Похудение и рельеф",8,3,ProgramLevel.Beginner,false,"Шаблон"),new(0,"Функциональная форма",10,3,ProgramLevel.Intermediate,false,"Шаблон"),new(0,"Набор массы",12,3,ProgramLevel.Advanced,false,"Шаблон")]
        : premium ? [new(0,"Сила и масса PRO",12,3,ProgramLevel.Advanced,true,"Масса|Сила|Верх тела"),new(0,"Эстетичное тело",8,3,ProgramLevel.Intermediate,true,"Рельеф|Всё тело|Дома"),new(0,"Сила атлета",16,3,ProgramLevel.Advanced,true,"Сила|Выносливость|Всё тело")]
        : [new(0,"Базовая сила",6,3,ProgramLevel.Beginner,false,"Для новичков"),new(0,"Сильное тело дома",4,3,ProgramLevel.Beginner,false,"Дома"),new(0,"Больше энергии",4,3,ProgramLevel.Intermediate,false,"Для прогресса")];
}
