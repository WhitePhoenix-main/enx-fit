using System.Text.Json;

namespace enx_fit.Services;

public sealed record DashboardWidgetDefinition(string Id, string Title, string Icon, string Description);
public sealed record DashboardWidgetPlacement(string Id, bool Visible, string Width);
public sealed record DashboardWidgetViewModel(Pages.DashboardModel Dashboard, string WidgetId);

public static class DashboardWidgets
{
    public const string WelcomeId = "welcome";

    public static readonly IReadOnlyList<DashboardWidgetDefinition> Catalog = [
        new("welcome", "Приветствие и цель", "target", "Приветствие, дата и текущая цель"),
        new("stat-volume", "Общий объём", "workout", "Краткий показатель нагрузки"),
        new("stat-workouts", "Количество тренировок", "shoe", "Занятия за выбранный период"),
        new("stat-weight", "Текущий вес", "scale", "Последний замер и изменение веса"),
        new("stat-streak", "Серия тренировок", "flame", "Тренировочные дни подряд"),
        new("stat-goal", "Новые рекорды", "trophy", "Новые максимумы за 30 дней"),
        new("weight", "Динамика веса", "bars", "График замеров за четыре недели"),
        new("records", "Силовые рекорды", "workout", "Лучшие рабочие подходы"),
        new("strength", "Рабочий вес", "trend", "Результаты упражнения за последние шесть недель"),
        new("goal", "План тренировок", "calendar", "Недельная цель и её выполнение"),
        new("volume", "Прогресс по объёму", "bars", "График тренировочной нагрузки"),
        new("workouts", "Последние тренировки", "menu", "История занятий и детали подходов"),
        new("recommendations", "Следующая цель", "target", "Предложение нагрузки, план и заметка тренера"),
        new("today", "Показатели дня", "calendar", "Тренировки, шаги и вода"),
        new("next", "Начать тренировку", "workout", "Быстрый старт или продолжение занятия"),
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
        var order = new[] { "welcome", "next", "goal", "recommendations", "workouts", "stat-workouts", "stat-goal", "stat-volume", "strength", "stat-weight", "habits", "records", "weight", "today", "sleep", "volume", "achievements", "calendar", "stat-streak" };
        return order.Select(id => new DashboardWidgetPlacement(id,
            id is not ("records" or "weight" or "today" or "sleep" or "volume" or "achievements" or "calendar" or "stat-streak"), "auto")).ToList();
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
