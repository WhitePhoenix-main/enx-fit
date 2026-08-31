using System.Text.Json;
using enx_fit.Models;
using enx_fit.Services;
using enx_fit.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Pages.Body;

public class IndexModel(
    BodyMeasurementService measurementService,
    CurrentUser currentUser,
    UserDirectoryService userDirectory) : PageModel
{
    public IReadOnlyList<BodyMeasurement> Measurements { get; private set; } = [];

    public string ChartDataJson { get; private set; } = "{}";

    [BindProperty(SupportsGet = true)]
    public string? UserId { get; set; }

    public bool IsAdministrator => currentUser.IsAdministrator;

    public bool CanShowCharts => !IsAdministrator || !string.IsNullOrWhiteSpace(UserId);

    public IReadOnlyList<UserOption> Users { get; private set; } = [];

    public IReadOnlyDictionary<string, string> OwnerNames { get; private set; } =
        new Dictionary<string, string>();

    public async Task OnGetAsync()
    {
        Measurements = await measurementService.GetAllAsync(UserId);

        if (IsAdministrator)
        {
            Users = await userDirectory.GetAllAsync();
            OwnerNames = await userDirectory.GetDisplayNamesAsync(Measurements.Select(measurement => measurement.UserId));
        }

        var chartMeasurements = Measurements
            .OrderBy(m => m.Date)
            .ThenBy(m => m.Id)
            .ToList();

        ChartDataJson = JsonSerializer.Serialize(new
        {
            labels = chartMeasurements.Select(m => m.Date.ToString("yyyy-MM-dd")),
            weights = chartMeasurements.Select(m => m.WeightKg),
            bmis = chartMeasurements.Select(m => m.Bmi)
        });
    }

    public string OwnerName(string? ownerId) =>
        string.IsNullOrWhiteSpace(ownerId)
            ? "Не назначен"
            : OwnerNames.GetValueOrDefault(ownerId, "Удалённый пользователь");
}
