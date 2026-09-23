using enx_fit.Areas.Identity.Data;
using enx_fit.Data;
using enx_fit.Models;
using enx_fit.Security;
using Microsoft.EntityFrameworkCore;

namespace enx_fit.Services;

public sealed class DashboardService(ApplicationDbContext db, CurrentUser currentUser)
{
    // Resolve the subject before reading any of their fitness data, on every request.
    public Task<ApplicationUser?> GetSubjectAsync(string? clientId)
    {
        var id = string.IsNullOrWhiteSpace(clientId) ? currentUser.Id : clientId;
        return db.Users.AsNoTracking().SingleOrDefaultAsync(user => user.Id == id &&
            (user.Id == currentUser.Id || currentUser.IsAdministrator ||
             ((currentUser.Role == UserRole.Trainer || currentUser.IsAdministrator) && user.TrainerId == currentUser.Id)));
    }

    public async Task<IReadOnlyList<ClientSummary>> GetClientsAsync(string? search = null)
    {
        if (currentUser.Role != UserRole.Trainer && !currentUser.IsAdministrator) return [];
        var users = db.Users.AsNoTracking().Where(user => user.TrainerId == currentUser.Id);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToUpperInvariant();
            users = users.Where(user => (user.NormalizedUserName ?? "").Contains(term) ||
                                       (user.NormalizedEmail ?? "").Contains(term));
        }
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await users.OrderBy(user => user.UserName).Select(user => new ClientSummary(
            user.Id, user.UserName ?? user.Email ?? "Клиент",
            db.WorkoutSessions.Count(workout => workout.UserId == user.Id && workout.Date <= today &&
                workout.WorkoutExercises.Any(exercise => exercise.SetEntries.Any(set => !set.IsWarmup && set.Reps > 0))),
            db.WorkoutSessions.Where(workout => workout.UserId == user.Id && workout.Date <= today &&
                workout.WorkoutExercises.Any(exercise => exercise.SetEntries.Any(set => !set.IsWarmup && set.Reps > 0)))
                .Select(workout => (DateOnly?)workout.Date).Max())).ToListAsync();
    }

    public async Task<DashboardData> LoadAsync(ApplicationUser subject, int days, DateOnly until)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var data = new DashboardData { Subject = subject, Today = today, Until = until, Days = days };
        data.Settings = await db.DashboardSettings.AsNoTracking().SingleOrDefaultAsync(s => s.UserId == subject.Id)
                        ?? new DashboardSettings { UserId = subject.Id };
        data.Workouts = await db.WorkoutSessions.AsNoTracking().AsSplitQuery().Where(w => w.UserId == subject.Id)
            .Include(w => w.WorkoutExercises).ThenInclude(e => e.Exercise)
            .Include(w => w.WorkoutExercises).ThenInclude(e => e.SetEntries)
            .OrderByDescending(w => w.Date).ThenByDescending(w => w.Id).ToListAsync();
        data.Measurements = await db.BodyMeasurements.AsNoTracking()
            .Where(m => m.UserId == subject.Id && m.Date <= until).OrderBy(m => m.Date).ThenBy(m => m.Id).ToListAsync();
        data.CheckIns = await db.DailyCheckIns.AsNoTracking()
            .Where(c => c.UserId == subject.Id && c.Date >= today.AddDays(-6) && c.Date <= today)
            .OrderBy(c => c.Date).ToListAsync();
        return data;
    }
}

public sealed record ClientSummary(string Id, string Name, int Workouts, DateOnly? LastWorkout);

public sealed class DashboardData
{
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
    public static IEnumerable<SetEntry> WorkingSets(WorkoutSession w) => w.WorkoutExercises.SelectMany(e => e.SetEntries).Where(s => !s.IsWarmup && s.Reps > 0);
    public static decimal Volume(WorkoutSession w) => WorkingSets(w).Sum(s => s.Weight * s.Reps);
    public IEnumerable<WorkoutSession> Completed => Workouts.Where(w => w.Date <= Today &&
        (!w.TrainingProgramId.HasValue || w.CompletedAtUtc.HasValue) && WorkingSets(w).Any());
    public IEnumerable<WorkoutSession> PeriodWorkouts => Completed.Where(w => w.Date >= Since && w.Date <= Until);
    public decimal TotalVolume => PeriodWorkouts.Sum(Volume);
    public decimal PreviousVolume => Completed.Where(w => w.Date >= Since.AddDays(-Days) && w.Date < Since).Sum(Volume);
    public decimal? VolumeChange => PreviousVolume > 0 ? Math.Round((TotalVolume / PreviousVolume - 1) * 100) : null;
    public decimal? Weight => Measurements.LastOrDefault()?.WeightKg;
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
    public WorkoutSession? NextWorkout => Workouts.Where(w => w.Date >= Today && !WorkingSets(w).Any()).OrderBy(w => w.Date).ThenBy(w => w.Id).FirstOrDefault();
    public DailyCheckIn TodayCheckIn => CheckIns.FirstOrDefault(c => c.Date == Today) ?? new DailyCheckIn { UserId = Subject.Id, Date = Today };
    public int HabitsDone => (TodayCheckIn.WaterMl >= 2000 ? 1 : 0) + (TodayCheckIn.Steps >= 8000 ? 1 : 0) + (TodayCheckIn.NutritionLogged ? 1 : 0) + (TodayCheckIn.StretchingDone ? 1 : 0);
    public int Streak
    {
        get
        {
            var dates = Completed.Select(w => w.Date).ToHashSet();
            var date = dates.Contains(Today) ? Today : Today.AddDays(-1);
            var count = 0;
            while (dates.Contains(date)) { count++; date = date.AddDays(-1); }
            return count;
        }
    }
    public IEnumerable<(string Name, decimal Weight)> Records => Completed.SelectMany(w => w.WorkoutExercises)
        .Where(e => e.SetEntries.Any(s => !s.IsWarmup && s.Reps > 0 && s.Weight > 0))
        .GroupBy(e => e.Exercise.Name).Select(g => (Name: g.Key, Weight: g.SelectMany(e => e.SetEntries).Where(s => !s.IsWarmup && s.Reps > 0).Max(s => s.Weight)))
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
                var workouts = PeriodWorkouts.Where(w => w.Date >= from && w.Date < to).ToList();
                yield return (from, workouts.Sum(Volume), workouts.Count);
            }
        }
    }
}
