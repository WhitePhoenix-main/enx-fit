using enx_fit.Extensions;
using enx_fit.Models;
using enx_fit.Pages;
using enx_fit.Services;

namespace enx_fit.ViewModels;

public sealed record ProgressPoint(DateOnly Date, decimal Value);
public sealed record ProgressExerciseOption(int Id, string Name);
public sealed record ProgressRecord(int ExerciseId, string Name, decimal Weight, DateOnly Date, int WorkoutId, string Photo);
public sealed record ProgressChart(string Id, string Label, IReadOnlyList<ProgressPoint> Points, bool Compact = false, bool DecimalLabels = false, (decimal Min, decimal Max, decimal Step)? FixedScale = null)
{
    public (decimal Min, decimal Max, decimal Step) Scale
    {
        get
        {
            if (Points.Count == 0) return (0, 30, 10);
            var min = Points.Min(p => p.Value); var max = Points.Max(p => p.Value);
            if (FixedScale is { } scale) return scale;
            var rough = Math.Max(1, (max - min) / 3);
            var power = (decimal)Math.Pow(10, Math.Floor(Math.Log10((double)rough)));
            var step = new[] { 1m, 2m, 5m, 10m }.First(v => v * power >= rough) * power;
            var lower = Math.Max(0, Math.Floor(min / step) * step - step);
            var upper = Math.Ceiling(max / step) * step + step;
            return (lower, Math.Max(lower + step * 3, upper), step);
        }
    }
}

public sealed class DashboardProgressViewModel
{
    public DateOnly Since { get; }
    public DateOnly Until { get; }
    public IReadOnlyList<ProgressExerciseOption> Exercises { get; }
    public int? ExerciseId { get; }
    public string ExerciseName { get; }
    public IReadOnlyList<ProgressPoint> Strength { get; }
    public IReadOnlyList<ProgressPoint> Body { get; }
    public IReadOnlyList<ProgressPoint> Volume { get; }
    public IReadOnlyList<ProgressRecord> Records { get; }
    public BodyMeasurement? Latest { get; }
    public BodyMeasurement? Previous { get; }
    public decimal? WorkingWeight { get; }
    public decimal? StrengthChange { get; }
    public decimal? WeightChange { get; }
    public int NewRecords { get; }
    public int WorkoutCount { get; }
    public decimal TotalVolume { get; }

    public DashboardProgressViewModel(DashboardModel model)
    {
        Until = model.Reference && !model.Request.Query.ContainsKey("Until") ? new(2026, 10, 2) : model.Data.Until;
        Since = model.Reference && model.ProgressWeeks == 6 && Until == new DateOnly(2026, 10, 2)
            ? new(2026, 8, 20) : Until.AddDays(1 - model.ProgressWeeks * 7);
        var chartSince = Until.AddDays(1 - model.ChartWeeks * 7);
        if (model.Reference)
        {
            Exercises = [new(1, "Жим лёжа"), new(2, "Приседания"), new(3, "Тяга")];
            ExerciseId = model.ProgressExercise ?? 1;
            ExerciseName = Exercises.FirstOrDefault(e => e.Id == ExerciseId)?.Name ?? "Упражнение не найдено";
            var dates = new[] { new DateOnly(2026, 8, 20), new(2026, 9, 3), new(2026, 9, 10), new(2026, 9, 17), new(2026, 9, 24), new(2026, 9, 30) };
            var values = ExerciseId switch { 1 => new[] { 55m, 55m, 57.5m, 57.5m, 60m, 60m }, 2 => [70m, 72.5m, 75m, 80m, 80m, 80m], 3 => [85m, 90m, 90m, 90m, 90m, 90m], _ => [] };
            var allStrength = dates.Take(values.Length).Select((date, i) => new ProgressPoint(date, values[i])).ToList();
            Strength = allStrength.Where(p => p.Date >= (model.ChartWeeks == 6 && Until == new DateOnly(2026, 10, 2) ? new DateOnly(2026, 8, 20) : chartSince) && p.Date <= Until).ToList();
            WorkingWeight = allStrength.Where(p => p.Date >= Since && p.Date <= Until).Select(p => (decimal?)p.Value).DefaultIfEmpty().Max();
            var periodStrength = allStrength.Where(p => p.Date >= Since && p.Date <= Until).ToList();
            StrengthChange = periodStrength.Count > 1 ? periodStrength[^1].Value - periodStrength[0].Value : null;
            var bodyValues = new[] { 80m, 79.6m, 79.1m, 78.7m, 78.4m, 78.2m };
            var allBody = dates.Select((date, i) => new BodyMeasurement { Id = -100 - i, Date = date, WeightKg = bodyValues[i], HeightCm = 180, WaistCm = 82, ChestCm = 101 }).Where(m => m.Date <= Until).ToList();
            Latest = allBody.LastOrDefault(); Previous = allBody.SkipLast(1).LastOrDefault();
            Body = allBody.Where(m => m.Date >= Since).Select(m => new ProgressPoint(m.Date, m.WeightKg)).ToList();
            WeightChange = Body.Count > 1 ? Body[^1].Value - Body[0].Value : null;
            Records = new[] { new ProgressRecord(1, "Жим лёжа", 60, new(2026, 9, 24), -1, "bench"), new(2, "Приседания", 80, new(2026, 9, 17), -2, "squat"), new(3, "Тяга", 90, new(2026, 9, 3), -3, "deadlift") }.Where(r => r.Date <= Until).ToList();
            NewRecords = model.ProgressWeeks == 6 ? 1 : Records.Count(r => r.Date >= Since);
            Volume = dates.Select((date, i) => new ProgressPoint(date, 2400 + i * 120)).Where(p => p.Date >= Since && p.Date <= Until).ToList();
            WorkoutCount = Volume.Count; TotalVolume = Volume.Sum(p => p.Value);
            return;
        }

        var completed = model.Data.Completed.Where(w => w.Date <= Until).OrderBy(w => w.Date).ThenBy(w => w.Id).ToList();
        var observations = completed.SelectMany(w => w.WorkoutExercises.Where(e => e.SetEntries.Any(ValidSet))
            .Select(e => new { w.Date, WorkoutId = w.Id, e.ExerciseId, Name = ExercisePresentation.DisplayName(e.Exercise), Weight = e.SetEntries.Where(ValidSet).Max(s => s.Weight) })).ToList();
        Exercises = observations.GroupBy(o => o.ExerciseId).Select(g => new ProgressExerciseOption(g.Key, g.Last().Name)).OrderBy(e => e.Id).ToList();
        ExerciseId = model.ProgressExercise ?? Exercises.FirstOrDefault()?.Id;
        ExerciseName = Exercises.FirstOrDefault(e => e.Id == ExerciseId)?.Name ?? (ExerciseId.HasValue ? "Упражнение не найдено" : "Рабочий вес");
        var selected = observations.Where(o => o.ExerciseId == ExerciseId).GroupBy(o => o.Date).OrderBy(g => g.Key).Select(g => new ProgressPoint(g.Key, g.Max(o => o.Weight))).ToList();
        Strength = selected.Where(p => p.Date >= chartSince).ToList();
        var period = selected.Where(p => p.Date >= Since).ToList();
        WorkingWeight = period.Select(p => (decimal?)p.Value).DefaultIfEmpty().Max();
        StrengthChange = period.Count > 1 ? period[^1].Value - period[0].Value : null;
        var measurements = model.Data.Measurements.Where(m => m.WeightKg > 0 && m.Date <= Until).OrderBy(m => m.Date).ThenBy(m => m.Id).ToList();
        Latest = measurements.LastOrDefault(); Previous = measurements.SkipLast(1).LastOrDefault();
        Body = measurements.Where(m => m.Date >= Since).GroupBy(m => m.Date).Select(g => new ProgressPoint(g.Key, g.Last().WeightKg)).ToList();
        WeightChange = Body.Count > 1 ? Body[^1].Value - Body[0].Value : null;
        Records = observations.GroupBy(o => o.ExerciseId).Select(g =>
        {
            var best = g.OrderByDescending(o => o.Weight).ThenBy(o => o.Date).ThenBy(o => o.WorkoutId).First();
            return new ProgressRecord(g.Key, best.Name, best.Weight, best.Date, best.WorkoutId, g.Key == 2 ? "squat" : g.Key == 3 ? "deadlift" : "bench");
        }).OrderByDescending(r => r.Date).ThenBy(r => r.ExerciseId).ToList();
        NewRecords = observations.GroupBy(o => o.ExerciseId).Count(g => g.Where(o => o.Date >= Since).Select(o => o.Weight).DefaultIfEmpty().Max() > g.Where(o => o.Date < Since).Select(o => o.Weight).DefaultIfEmpty().Max());
        var periodWorkouts = completed.Where(w => w.Date >= Since).ToList();
        Volume = periodWorkouts.GroupBy(w => w.Date).OrderBy(g => g.Key).Select(g => new ProgressPoint(g.Key, g.Sum(DashboardData.Volume))).ToList();
        WorkoutCount = periodWorkouts.Count; TotalVolume = periodWorkouts.Sum(DashboardData.Volume);
    }

    private static bool ValidSet(SetEntry set) => set.IsCompleted && !set.IsWarmup && set.Reps > 0 && set.Weight > 0;
}
