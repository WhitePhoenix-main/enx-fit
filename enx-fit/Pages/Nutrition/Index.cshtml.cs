using System.ComponentModel.DataAnnotations;
using enx_fit.Models;
using enx_fit.Security;
using enx_fit.Services.Dashboard;
using enx_fit.Services.Nutrition;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Pages.Nutrition;

[MinimumRole(UserRole.User)]
[RequestSizeLimit(16384)]
public sealed class IndexModel(NutritionService nutrition, DashboardService dashboard, CurrentUser currentUser, IFeatureAccessService features) : PageModel
{
    [BindProperty(SupportsGet = true)] public DateOnly? Day { get; set; }
    [BindProperty(SupportsGet = true), StringLength(450)] public string? ClientId { get; set; }
    [BindProperty] public MealInput Meal { get; set; } = new();
    [BindProperty] public GoalsInput Goals { get; set; } = new();
    public NutritionDay Data { get; private set; } = null!;
    public IReadOnlyList<NutritionHistoryDay> History { get; private set; } = [];
    public IReadOnlyList<ClientSummary> Clients { get; private set; } = [];
    public bool Coach { get; private set; }
    public bool ViewingClient => !string.IsNullOrWhiteSpace(ClientId) && ClientId != currentUser.Id;
    public string SubjectName { get; private set; } = "";
    public DateOnly Today => currentUser.LocalToday;
    public string ViewerId => currentUser.Id;
    private string SubjectId => ViewingClient ? ClientId! : currentUser.Id;
    [TempData] public string? StatusMessage { get; set; }

    private async Task<bool> LoadAsync()
    {
        Coach = await features.CanUseAsync(Feature.CoachClientManagement);
        if (Coach)
        {
            Clients = await dashboard.GetClientsAsync();
            if (ClientId is null && Clients.Count > 0) ClientId = Clients[0].Id;
        }
        if (ViewingClient && !Coach) return false;
        var subject = await dashboard.GetSubjectAsync(ClientId);
        if (subject is null) return false;
        SubjectName = subject.UserName ?? "Клиент";
        if (Day is null || Day < Today.AddYears(-2) || Day > Today) Day = Today;
        Data = await nutrition.LoadAsync(subject.Id, Day.Value);
        if (Coach) History = await nutrition.HistoryAsync(subject.Id, Day.Value);
        if (!string.Equals(Request.Query["handler"], "Goals", StringComparison.OrdinalIgnoreCase)) Goals = GoalsInput.From(Data.Settings);
        return true;
    }
    public async Task<IActionResult> OnGetAsync()
    {
        if (!await LoadAsync()) return NotFound();
        Goals = GoalsInput.From(Data.Settings);
        return Page();
    }
    public async Task<IActionResult> OnPostMealAsync()
    {
        ModelState.Clear();
        if (!await LoadAsync()) return NotFound();
        if (ViewingClient) return Forbid();
        if (!TryValidateModel(Meal, nameof(Meal))) return Page();
        await nutrition.SaveMealAsync(new() { UserId = SubjectId, Date = Day!.Value, Time = Meal.Time, Type = Meal.Type, Name = Meal.Name.Trim(), Calories = Meal.Calories, Protein = Meal.Protein, Fat = Meal.Fat, Carbs = Meal.Carbs });
        StatusMessage = "Приём пищи сохранён.";
        return RedirectToPage(new { Day = Day!.Value.ToString("yyyy-MM-dd"), ClientId });
    }
    public async Task<IActionResult> OnPostWaterAsync()
    {
        if (!await LoadAsync()) return NotFound();
        if (ViewingClient) return Forbid();
        await nutrition.AddWaterAsync(SubjectId, Day!.Value);
        return RedirectToPage(new { Day = Day.Value.ToString("yyyy-MM-dd") });
    }
    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        if (!await LoadAsync()) return NotFound();
        if (ViewingClient) return Forbid();
        if (!await nutrition.DeleteMealAsync(SubjectId, id, Day!.Value)) return NotFound();
        StatusMessage = "Приём пищи удалён.";
        return RedirectToPage(new { Day = Day!.Value.ToString("yyyy-MM-dd") });
    }
    public async Task<IActionResult> OnPostGoalsAsync()
    {
        ModelState.Clear();
        if (!await LoadAsync()) return NotFound();
        if (!TryValidateModel(Goals, nameof(Goals))) return Page();
        await nutrition.SaveGoalsAsync(new() { UserId = SubjectId, Goal = Goals.Title.Trim(), Calories = Goals.Calories, Protein = Goals.Protein, Fat = Goals.Fat, Carbs = Goals.Carbs, WaterMl = Goals.WaterMl });
        StatusMessage = ViewingClient ? "Цели питания клиента сохранены." : "Цели питания сохранены.";
        return RedirectToPage(new { Day = Day!.Value.ToString("yyyy-MM-dd"), ClientId });
    }
    public sealed class MealInput
    {
        [Required, StringLength(120, MinimumLength = 1)] public string Name { get; set; } = "";
        [RegularExpression("^(Завтрак|Обед|Ужин|Перекус)$")] public string Type { get; set; } = "Завтрак";
        public TimeOnly Time { get; set; } = new(8, 0);
        [Range(0, 10000)] public int Calories { get; set; }
        [Range(0, 1000)] public int Protein { get; set; }
        [Range(0, 1000)] public int Fat { get; set; }
        [Range(0, 1500)] public int Carbs { get; set; }
    }
    public sealed class GoalsInput
    {
        [Required, StringLength(120)] public string Title { get; set; } = "Сбалансированное питание";
        [Range(800, 10000)] public int Calories { get; set; } = 2200;
        [Range(0, 500)] public int Protein { get; set; } = 120;
        [Range(0, 500)] public int Fat { get; set; } = 70;
        [Range(0, 1500)] public int Carbs { get; set; } = 250;
        [Range(250, 10000)] public int WaterMl { get; set; } = 2500;
        public static GoalsInput From(NutritionSettings s) => new() { Title = s.Goal, Calories = s.Calories, Protein = s.Protein, Fat = s.Fat, Carbs = s.Carbs, WaterMl = s.WaterMl };
    }
}
