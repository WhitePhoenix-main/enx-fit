using enx_fit.Areas.Identity.Data;
using enx_fit.Models;
using enx_fit.Presentation.Exercises;

namespace enx_fit.Services.Dashboard;

public sealed record ClientSummary(string Id, string Name, int Workouts, DateOnly? LastWorkout);
public sealed record DashboardStrengthPoint(DateOnly Date, decimal Weight);
public sealed record DashboardStrengthSeries(Exercise Exercise, IReadOnlyList<DashboardStrengthPoint> Points);

public sealed class DashboardData
{
    public DashboardHistorySummary? History { get; init; }
    public ApplicationUser Subject { get; init; } = null!;
    public DashboardSettings Settings { get; set; } = new();
    public List<WorkoutSession> Workouts { get; set; } = [];
    public List<BodyMeasurement> Measurements { get; set; } = [];
    public List<DailyCheckIn> CheckIns { get; set; } = [];
    public DateOnly Today { get; init; }
    public DateOnly Until { get; init; }
    public int Days { get; init; }
    public DateOnly Since => Until.AddDays(1 - Days);
    public string Name => (Subject.UserName ?? Subject.Email)?.Split('@')[0] is { Length: > 0 } name ? name : "Спортсмен";
    public string Initial => Name[..1].ToUpperInvariant();
    public static IEnumerable<SetEntry> WorkingSets(WorkoutSession w) => w.WorkoutExercises.SelectMany(e => e.SetEntries).Where(s => s.IsCompleted && !s.IsWarmup && s.Reps > 0);
    public static decimal Volume(WorkoutSession w) => WorkingSets(w).Sum(s => s.Weight * s.Reps);
    // Detailed sessions only. Home widgets must use the summary fields for all-time totals.
    public IEnumerable<WorkoutSession> Completed => Workouts.Where(w => w.Date <= Today &&
        w.State == WorkoutStatus.Completed && WorkingSets(w).Any());
    public IEnumerable<WorkoutSession> PeriodWorkouts => Completed.Where(w => w.Date >= Since && w.Date <= Until);
    public int CompletedCount => History?.Days.Sum(d => d.Count) ?? Completed.Count();
    public int CountCompleted(DateOnly from, DateOnly to) => History is { } history
        ? history.Days.Where(d => d.Date >= from && d.Date <= to).Sum(d => d.Count)
        : Completed.Count(w => w.Date >= from && w.Date <= to);
    public IEnumerable<DateOnly> CompletedDates => History?.Days.Select(d => d.Date) ?? Completed.Select(w => w.Date);
    private decimal VolumeBetween(DateOnly from, DateOnly to) => History is { } history
        ? history.Days.Where(d => d.Date >= from && d.Date <= to).Sum(d => d.Volume)
        : Completed.Where(w => w.Date >= from && w.Date <= to).Sum(Volume);
    public decimal TotalVolume => VolumeBetween(Since, Until);
    public decimal PreviousVolume => VolumeBetween(Since.AddDays(-Days), Since.AddDays(-1));
    public decimal? VolumeChange => PreviousVolume > 0 ? Math.Round((TotalVolume / PreviousVolume - 1) * 100) : null;
    public decimal? Weight => Measurements.LastOrDefault()?.WeightKg;
    public int NewRecords => History?.Exercises.Count(e => e.Recent > e.Prior) ?? Completed.SelectMany(w => w.WorkoutExercises.Select(e => new { w.Date, e.ExerciseId,
            Weight = e.SetEntries.Where(s => s.IsCompleted && !s.IsWarmup && s.Reps > 0).Select(s => s.Weight).DefaultIfEmpty().Max() }))
        .GroupBy(e => e.ExerciseId).Count(g => g.Where(e => e.Date >= Today.AddDays(-29)).Select(e => e.Weight).DefaultIfEmpty().Max() >
            g.Where(e => e.Date < Today.AddDays(-29)).Select(e => e.Weight).DefaultIfEmpty().Max());
    public decimal? WeightChange
    {
        get
        {
            var recent = Measurements.Where(m => m.Date >= Until.AddDays(-28)).ToList();
            return recent.Count > 1 ? recent[^1].WeightKg - recent[0].WeightKg : null;
        }
    }
    public IEnumerable<WorkoutSession> ThisWeek => Completed.Where(w => w.Date >= Today.AddDays(-((int)Today.DayOfWeek + 6) % 7));
    public int GoalPercent => Math.Min(100, (int)Math.Round(ThisWeek.Count() * 100d / Settings.WeeklyWorkoutGoal));
    public WorkoutSession? NextWorkout => Workouts.Where(w => w.Date >= Today && w.State == WorkoutStatus.Planned).OrderBy(w => w.Date).ThenBy(w => w.Id).FirstOrDefault();
    public DailyCheckIn TodayCheckIn => CheckIns.FirstOrDefault(c => c.Date == Today) ?? new DailyCheckIn { UserId = Subject.Id, Date = Today };
    public int HabitsDone => (TodayCheckIn.WaterMl >= 2000 ? 1 : 0) + (TodayCheckIn.Steps >= 8000 ? 1 : 0) + (TodayCheckIn.NutritionLogged ? 1 : 0) + (TodayCheckIn.StretchingDone ? 1 : 0);
    public int Streak
    {
        get
        {
            var dates = CompletedDates.ToHashSet();
            var date = dates.Contains(Today) ? Today : Today.AddDays(-1);
            var count = 0;
            while (dates.Contains(date)) { count++; date = date.AddDays(-1); }
            return count;
        }
    }
    // Observed working weights only: no warmups, unfinished sessions or projected values.
    public DashboardStrengthSeries? Strength => StrengthFor(null);
    public DashboardStrengthSeries? StrengthFor(Guid? exerciseId)
    {
        var sessions = Completed.Where(w => w.Date >= Until.AddDays(-41) && w.Date <= Until)
            .OrderByDescending(w => w.Date).ThenByDescending(w => w.Id).ToList();
        var exercise = sessions.SelectMany(w => w.WorkoutExercises.OrderBy(e => e.Order))
            .FirstOrDefault(e => (!exerciseId.HasValue || e.ExerciseId == exerciseId) && e.SetEntries.Any(s => s.IsCompleted && !s.IsWarmup && s.Reps > 0 && s.Weight > 0));
        if (exercise is null) return null;
        var points = sessions.SelectMany(w => w.WorkoutExercises.Where(e => e.ExerciseId == exercise.ExerciseId)
                .SelectMany(e => e.SetEntries.Where(s => s.IsCompleted && !s.IsWarmup && s.Reps > 0 && s.Weight > 0)
                    .Select(s => new { w.Date, s.Weight })))
            .GroupBy(s => s.Date).OrderBy(g => g.Key)
            .Select(g => new DashboardStrengthPoint(g.Key, g.Max(s => s.Weight))).ToList();
        return new(exercise.Exercise, points);
    }
    public IEnumerable<(string Name, decimal Weight, decimal? Gain)> Records => History?.Records.AsEnumerable() ?? Completed
        .SelectMany(w => w.WorkoutExercises.Select(e => new { w.Date, Exercise = e }))
        .Where(x => x.Exercise.SetEntries.Any(s => s.IsCompleted && !s.IsWarmup && s.Reps > 0 && s.Weight > 0))
        .GroupBy(x => x.Exercise.ExerciseId).Select(g =>
        {
            var best = g.SelectMany(x => x.Exercise.SetEntries).Where(s => s.IsCompleted && !s.IsWarmup && s.Reps > 0).Max(s => s.Weight);
            var prior = g.Where(x => x.Date < Today.AddDays(-29)).SelectMany(x => x.Exercise.SetEntries)
                .Where(s => s.IsCompleted && !s.IsWarmup && s.Reps > 0).Select(s => s.Weight).DefaultIfEmpty().Max();
            return (Name: enx_fit.Presentation.Exercises.ExercisePresentation.DisplayName(g.First().Exercise.Exercise),
                Weight: best, Gain: prior > 0 && best > prior ? (decimal?)(best - prior) : null);
        })
        .OrderByDescending(r => r.Weight);
    public IEnumerable<(DateOnly Date, decimal Volume, int Count)> Chart
    {
        get
        {
            var buckets = Days == 7 ? 7 : Days == 30 ? 10 : 12;
            for (var i = 0; i < buckets; i++)
            {
                var from = Since.AddDays(i * Days / buckets);
                var to = Since.AddDays((i + 1) * Days / buckets);
                yield return (from, VolumeBetween(from, to.AddDays(-1)), CountCompleted(from, to.AddDays(-1)));
            }
        }
    }
}
