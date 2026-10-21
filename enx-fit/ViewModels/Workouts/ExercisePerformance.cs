using enx_fit.Models;

namespace enx_fit.ViewModels.Workouts;

public sealed record PerformanceDrop(decimal Weight, int Reps, bool IsBodyweight);
public sealed record PerformanceSet(int Number, decimal Weight, int Reps, bool IsBodyweight, decimal Volume, IReadOnlyList<PerformanceDrop> Drops);
public sealed record ExerciseHistorySession(int WorkoutId, DateOnly Date, string Title, IReadOnlyList<PerformanceSet> Sets)
{
    public decimal Volume => Sets.Sum(s => s.Volume);
}
public sealed record ExercisePerformance(Exercise Exercise, IReadOnlyList<ExerciseHistorySession> History)
{
    public decimal BestWeight => History.SelectMany(h => h.Sets).Where(s => !s.IsBodyweight).Select(s => s.Weight).DefaultIfEmpty().Max();
    public decimal BestSetVolume => History.SelectMany(h => h.Sets).Select(s => s.Volume).DefaultIfEmpty().Max();
    public int BestBodyweightReps => History.SelectMany(h => h.Sets).Where(s => s.IsBodyweight).Select(s => s.Reps).DefaultIfEmpty().Max();
}
