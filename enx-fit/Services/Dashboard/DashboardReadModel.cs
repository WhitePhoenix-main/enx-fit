using enx_fit.Models;
using enx_fit.Presentation.Exercises;

namespace enx_fit.Services.Dashboard;

public sealed record DashboardDay(DateOnly Date, int Count, decimal Volume);
public sealed record DashboardExercise(Guid Id, string Name, decimal Best, decimal Prior, decimal Recent,
    DateOnly? ProgressDate, int? ProgressWorkoutId);

// Shared widgets consume DashboardData; only the home query supplies these SQL summaries.
// Detailed sessions are a separate, bounded collection, never the source of all-time totals.
public sealed class DashboardHistorySummary(IReadOnlyList<DashboardDay> days, IReadOnlyList<DashboardExercise> exercises)
{
    public IReadOnlyList<DashboardDay> Days { get; } = days;
    public IReadOnlyList<DashboardExercise> Exercises { get; } = exercises;
    public IReadOnlyList<(string Name, decimal Weight, decimal? Gain)> Records { get; } = exercises
        .Where(e => e.Best > 0).OrderByDescending(e => e.Best)
        .Select(e => (ExercisePresentation.DisplayName(new Exercise { Id = e.Id, Name = e.Name }),
            e.Best, e.Prior > 0 && e.Best > e.Prior ? (decimal?)(e.Best - e.Prior) : null)).ToList();
}

public sealed record DashboardReadModel(DashboardData Data, string? WidgetsJson, bool UsesDefaultLayout);
