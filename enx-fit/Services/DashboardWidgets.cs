using System.Text.Json;

namespace enx_fit.Services;

public sealed record DashboardWidgetDefinition(string Id, string Title, string Icon, string Description);
public sealed record DashboardWidgetPlacement(string Id, bool Visible, string Width);
public sealed record DashboardWidgetViewModel(Pages.DashboardModel Dashboard, string WidgetId);

public static class DashboardWidgets
{
    public const string WelcomeId = "welcome";

    public static readonly IReadOnlyList<DashboardWidgetDefinition> Catalog = [
        new("welcome", "Приветствие и цель", "target", "Ваша цель и восстановление"),
        new("stat-volume", "Общий объём", "workout", "Краткий показатель нагрузки"),
        new("stat-workouts", "Количество тренировок", "shoe", "Занятия за выбранный период"),
        new("stat-weight", "Текущий вес", "scale", "Последний замер и изменение веса"),
        new("stat-streak", "Серия тренировок", "flame", "Тренировочные дни подряд"),
        new("stat-goal", "Процент цели", "target", "Краткий прогресс за неделю"),
        new("weight", "Динамика веса", "bars", "График замеров за четыре недели"),
        new("records", "Силовые рекорды", "workout", "Лучшие рабочие подходы"),
        new("goal", "План тренировок", "calendar", "Недельная цель и её выполнение"),
        new("volume", "Прогресс по объёму", "bars", "График тренировочной нагрузки"),
        new("workouts", "Последние тренировки", "menu", "История занятий и детали подходов"),
        new("recommendations", "Рекомендации", "spark", "Подсказки и заметка тренера"),
        new("today", "Показатели дня", "calendar", "Тренировки, шаги и вода"),
        new("next", "Ближайшая тренировка", "workout", "Следующее занятие в плане"),
        new("sleep", "Сон и восстановление", "moon", "Продолжительность сна за неделю"),
        new("habits", "Привычки", "activity", "Ежедневные отметки"),
        new("achievements", "Достижения", "trophy", "Серии и личные рекорды"),
        new("calendar", "Календарь", "calendar", "Тренировочные дни месяца")
    ];

    public static readonly IReadOnlyDictionary<string, string> Widths = new Dictionary<string, string>
    {
        ["auto"] = "Авто", ["compact"] = "Узкий", ["medium"] = "Средний",
        ["wide"] = "Широкий", ["full"] = "Во всю строку"
    };
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static List<DashboardWidgetPlacement> Defaults(bool coach)
    {
        var order = coach
            ? new[] { "welcome", "stat-volume", "stat-workouts", "stat-weight", "stat-streak", "stat-goal", "volume", "goal", "records", "next", "workouts", "calendar", "recommendations", "weight", "today", "sleep", "habits", "achievements" }
            : new[] { "welcome", "today", "weight", "records", "goal", "next", "workouts", "sleep", "recommendations", "habits", "volume", "achievements", "calendar", "stat-weight", "stat-streak", "stat-volume", "stat-workouts", "stat-goal" };
        return order.Select(id => new DashboardWidgetPlacement(id, coach || !id.StartsWith("stat-"), id switch
        {
            "welcome" => coach ? "full" : "wide",
            "workouts" => coach ? "medium" : "wide",
            "volume" => "medium",
            "recommendations" => "wide",
            _ when id.StartsWith("stat-") => "auto",
            _ => "compact"
        })).ToList();
    }

    public static bool IsValid(IReadOnlyList<DashboardWidgetPlacement>? widgets) =>
        widgets is not null && widgets.Count == Catalog.Count &&
        widgets.Select(w => w?.Id).Distinct(StringComparer.Ordinal).Count() == Catalog.Count &&
        widgets.All(w => w is not null && Catalog.Any(c => c.Id == w.Id) &&
                         w.Width is not null && Widths.ContainsKey(w.Width));

    public static List<DashboardWidgetPlacement> Read(string? json, bool coach)
    {
        var defaults = Defaults(coach);
        if (string.IsNullOrWhiteSpace(json)) return defaults;
        try
        {
            var saved = JsonSerializer.Deserialize<List<DashboardWidgetPlacement>>(json, JsonOptions);
            if (saved is null) return defaults;
            var result = saved.Where(w => w is not null && Catalog.Any(c => c.Id == w.Id) &&
                            w.Width is not null && Widths.ContainsKey(w.Width))
                .DistinctBy(w => w.Id).ToList();
            // Newly introduced widgets receive their defaults without disturbing saved order.
            result.AddRange(defaults.Where(w => result.All(existing => existing.Id != w.Id)));
            return PinWelcomeFirst(result);
        }
        catch (JsonException) { return defaults; }
    }

    // Preserve all other choices while repairing layouts saved before the greeting was pinned.
    public static List<DashboardWidgetPlacement> PinWelcomeFirst(IEnumerable<DashboardWidgetPlacement> widgets) =>
        widgets.OrderBy(widget => widget.Id != WelcomeId).ToList();

    public static string Serialize(IReadOnlyList<DashboardWidgetPlacement> widgets) =>
        JsonSerializer.Serialize(widgets, JsonOptions);
}
