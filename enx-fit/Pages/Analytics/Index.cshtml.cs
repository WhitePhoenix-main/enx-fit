using System.Text.Json;
using enx_fit.Models;
using enx_fit.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Pages.Analytics;

public class IndexModel(
    AnalyticsDataService analyticsDataService,
    TrainingAnalyticsService trainingAnalyticsService) : PageModel
{
    public IReadOnlyList<Exercise> Exercises { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public int? ExerciseId { get; set; }

    public BestExerciseSet? BestSet { get; private set; }

    public string ProgressText { get; private set; } = string.Empty;

    public string ChartDataJson { get; private set; } = "{}";

    public bool HasEnoughData { get; private set; }

    public async Task OnGetAsync()
    {
        Exercises = await analyticsDataService.GetExercisesAsync();

        if (!ExerciseId.HasValue || Exercises.All(exercise => exercise.Id != ExerciseId.Value))
        {
            ExerciseId = null;
            return;
        }

        var workouts = await analyticsDataService.GetWorkoutsForExerciseAsync(ExerciseId.Value);
        var workingWeightTrend = trainingAnalyticsService.GetWorkingWeightTrend(workouts, ExerciseId.Value);
        var estimatedOneRepMaxTrend = trainingAnalyticsService.GetEstimatedOneRepMaxTrend(workouts, ExerciseId.Value);
        var volumeTrend = trainingAnalyticsService.GetExerciseVolumeTrend(workouts, ExerciseId.Value);

        BestSet = trainingAnalyticsService.GetBestSetForExercise(workouts, ExerciseId.Value);
        ProgressText = trainingAnalyticsService.GetExerciseProgressText(workouts, ExerciseId.Value);
        HasEnoughData = workingWeightTrend.Count >= 2;

        ChartDataJson = JsonSerializer.Serialize(new
        {
            workingWeight = new
            {
                labels = workingWeightTrend.Select(point => point.Date.ToString("yyyy-MM-dd")),
                values = workingWeightTrend.Select(point => point.Weight)
            },
            estimatedOneRepMax = new
            {
                labels = estimatedOneRepMaxTrend.Select(point => point.Date.ToString("yyyy-MM-dd")),
                values = estimatedOneRepMaxTrend.Select(point => point.EstimatedOneRepMax)
            },
            volume = new
            {
                labels = volumeTrend.Select(point => point.Date.ToString("yyyy-MM-dd")),
                values = volumeTrend.Select(point => point.Volume)
            }
        });
    }
}
