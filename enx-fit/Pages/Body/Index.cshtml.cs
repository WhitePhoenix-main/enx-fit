using System.Text.Json;
using enx_fit.Models;
using enx_fit.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Pages.Body;

public class IndexModel(BodyMeasurementService measurementService) : PageModel
{
    public IReadOnlyList<BodyMeasurement> Measurements { get; private set; } = [];

    public string ChartDataJson { get; private set; } = "{}";

    public async Task OnGetAsync()
    {
        Measurements = await measurementService.GetAllAsync();

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
}
