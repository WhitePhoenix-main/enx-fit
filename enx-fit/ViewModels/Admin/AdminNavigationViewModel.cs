namespace enx_fit.ViewModels.Admin;

public sealed record AdminSection(string Page, string Label, string Icon, string[] Prefixes)
{
    public bool IsActive(string page) => Prefixes.Any(prefix => page.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
}

public sealed record AdminNavigationViewModel(string Page, string AccountName, string Initial)
{
    public static readonly AdminSection[] Sections = [
        new("/Admin/Index", "Обзор", "grid", ["/Admin/Index"]),
        new("/Admin/Users/Index", "Пользователи", "users", ["/Admin/Users/"]),
        new("/Training/Workouts/Index", "Тренировки", "workout", ["/Training/Workouts/", "/Progress/ExerciseAnalytics/"]),
        new("/Admin/Exercises/Index", "Упражнения", "book", ["/Admin/Exercises/", "/Training/Exercises/"]),
        new("/Progress/Measurements/Index", "Замеры", "activity", ["/Progress/Measurements/"])
    ];
    public string SectionLabel => Sections.FirstOrDefault(section => section.IsActive(Page))?.Label ?? "Обзор";
}
