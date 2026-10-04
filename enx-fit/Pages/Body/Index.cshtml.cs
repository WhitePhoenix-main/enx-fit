using System.Globalization;
using enx_fit.Models;
using enx_fit.Services;
using enx_fit.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Pages.Body;

[MinimumRole(UserRole.User)]
public class IndexModel(BodyMeasurementService measurementService, CurrentUser currentUser,
    UserDirectoryService userDirectory) : PageModel
{
    public IReadOnlyList<BodyMeasurement> Measurements { get; private set; } = [];
    public IReadOnlyList<BodyMeasurement> ChartMeasurements { get; private set; } = [];
    public BodyMeasurement? Latest { get; private set; }
    public BodyMeasurement? Previous { get; private set; }
    public BodyMeasurement? Selected { get; private set; }
    [BindProperty(SupportsGet = true)] public string? UserId { get; set; }
    [BindProperty(SupportsGet = true)] public string Metric { get; set; } = "weight";
    [BindProperty(SupportsGet = true)] public int Weeks { get; set; } = 12;
    [BindProperty(SupportsGet = true)] public int? Point { get; set; }
    public bool IsAdministrator => currentUser.IsAdministrator;
    public bool CanShowCharts => !IsAdministrator || !string.IsNullOrWhiteSpace(UserId);
    public IReadOnlyList<UserOption> Users { get; private set; } = [];
    public IReadOnlyDictionary<string, string> OwnerNames { get; private set; } = new Dictionary<string, string>();
    public string MetricTitle => Metric switch { "waist" => "Талия", "chest" => "Грудь", "hip" => "Ягодицы", _ => "Вес" };
    public string Unit => Metric == "weight" ? "кг" : "см";
    public decimal? Value(BodyMeasurement item) => Metric switch { "waist" => item.WaistCm, "chest" => item.ChestCm, "hip" => item.HipCm, _ => item.WeightKg };
    public static string Number(decimal value) => value.ToString("0.##", CultureInfo.GetCultureInfo("ru-RU"));
    public string OwnerName(string? id) => string.IsNullOrWhiteSpace(id) ? "Не назначен" : OwnerNames.GetValueOrDefault(id, "Удалённый пользователь");

    public async Task<IActionResult> OnGetAsync()
    {
        if (Request.Query.ContainsKey("ClientId")) return NotFound();
        if (Metric is not ("weight" or "waist" or "chest" or "hip")) Metric = "weight";
        if (Weeks is not (0 or 4 or 12 or 52)) Weeks = 12;
        if (!IsAdministrator) UserId = null;
        else if (!string.IsNullOrWhiteSpace(UserId) && !await userDirectory.ExistsAsync(UserId)) return NotFound();
        Measurements = await measurementService.GetAllAsync(UserId);
        if (IsAdministrator)
        {
            Users = await userDirectory.GetAllAsync();
            OwnerNames = await userDirectory.GetDisplayNamesAsync(Measurements.Select(m => m.UserId));
        }
        if (CanShowCharts)
        {
            var actual = Measurements.Where(m => m.Date <= currentUser.LocalToday).ToList();
            Latest = actual.FirstOrDefault(); Previous = actual.Skip(1).FirstOrDefault();
            ChartMeasurements = actual.Where(m => Value(m).HasValue && (Weeks == 0 || m.Date >= currentUser.LocalToday.AddDays(-Weeks * 7 + 1)))
                .OrderBy(m => m.Date).ThenBy(m => m.Id).ToList();
            Selected = ChartMeasurements.FirstOrDefault(m => m.Id == Point) ?? ChartMeasurements.LastOrDefault();
        }
        // The GET filters are normalized above; render those values rather than rejected query text.
        ModelState.Clear();
        Response.Headers.CacheControl = "no-store";
        return Page();
    }
}
