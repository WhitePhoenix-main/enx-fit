namespace enx_fit.ViewModels;

public sealed record AdminSection(string Page, string Label, string Icon, string[] Prefixes)
{
    public bool IsActive(string page) => Prefixes.Any(prefix => page.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
}

public sealed record AdminNavigationViewModel(string Page, string AccountName, string Initial)
{
    public static readonly AdminSection[] Sections = [
        new("/Admin/Index", "Обзор", "grid", ["/Admin/Index"]),
        new("/Admin/Users/Index", "Пользователи", "users", ["/Admin/Users/"]),
        new("/Workouts/Index", "Тренировки", "workout", ["/Workouts/", "/Analytics/"]),
        new("/Admin/Exercises/Index", "Упражнения", "book", ["/Admin/Exercises/", "/Exercises/"]),
        new("/Body/Index", "Замеры", "activity", ["/Body/"])
    ];
    public string SectionLabel => Sections.FirstOrDefault(section => section.IsActive(Page))?.Label ?? "Обзор";
}
