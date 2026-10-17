namespace enx_fit.Presentation.Dashboard;

public sealed record DashboardSection(string Page, string Title, string Icon, string Description,
    IReadOnlyList<DashboardWidgetPlacement> Widgets);

public static class DashboardSections
{
    public static readonly IReadOnlyList<DashboardSection> All = [
        new("/Training/Index", "Тренировки", "workout", "История занятий и ближайшая тренировка.", [
            new("stat-workouts", true, "compact"), new("next", true, "wide"),
            new("workouts", true, "full"), new("calendar", true, "medium")]),
        new("/Progress/Analytics", "Аналитика", "bars", "Тренировочная нагрузка и динамика за выбранный период.", [
            new("stat-volume", true, "medium"), new("stat-workouts", true, "medium"),
            new("volume", true, "full")]),
        new("/Training/Plans", "Планы", "calendar", "Ваша цель, расписание и рекомендации.", [
            new("goal", true, "medium"), new("next", true, "medium"),
            new("calendar", true, "medium"), new("recommendations", true, "full")]),
        new("/Dashboard/Habits", "Привычки", "nutrition", "Ежедневная активность, сон и восстановление.", [
            new("today", true, "medium"), new("sleep", true, "medium"), new("habits", true, "full")]),
        new("/Progress/Index", "Прогресс", "trend", "Изменения веса и силовые показатели.", [
            new("stat-weight", true, "compact"), new("weight", true, "wide"), new("records", true, "full")]),
        new("/Progress/Achievements", "Достижения", "trophy", "Серии тренировок, выполненные цели и личные рекорды.", [
            new("stat-streak", true, "medium"), new("stat-goal", true, "medium"),
            new("achievements", true, "medium"), new("records", true, "medium")])
    ];

    public static DashboardSection? Find(string page) =>
        All.FirstOrDefault(section => string.Equals(section.Page, page, StringComparison.OrdinalIgnoreCase));
}
