using enx_fit.Areas.Identity.Data;
using enx_fit.Data;
using enx_fit.Models;
using enx_fit.Security;
using enx_fit.Services.Workouts;
using Microsoft.EntityFrameworkCore;

namespace enx_fit.Services.Dashboard;

// The home screen has its own read path. History/progress screens retain their full data contract.
public sealed class DashboardQuery(ApplicationDbContext db, CurrentUser currentUser)
{
    internal async Task<DashboardReadModel> LoadAsync(ApplicationUser subject, int days, DateOnly until,
        int weekOffset, int chartWeeks)
    {
        var today = currentUser.LocalToday;
        var mode = subject.Id == currentUser.Id ? "personal" : "coach";
        var config = await (from settings in db.DashboardSettings.AsNoTracking().Where(s => s.UserId == subject.Id).DefaultIfEmpty()
            from layout in db.DashboardLayoutPreferences.Where(l => l.UserId == currentUser.Id && l.Mode == mode).DefaultIfEmpty()
            select new { Settings = settings, Layout = layout }).SingleAsync();

        var completed = db.WorkoutSessions.AsNoTracking().Where(w => w.UserId == subject.Id && w.Date <= today)
            .Where(WorkoutStates.Completed).Where(w => w.WorkoutExercises.Any(e =>
                e.SetEntries.Any(s => s.IsCompleted && !s.IsWarmup && s.Reps > 0)));
        var daily = await completed.GroupBy(w => w.Date).Select(g => new DashboardDay(g.Key, g.Count(),
            g.Sum(w => w.WorkoutExercises.SelectMany(e => e.SetEntries)
                .Where(s => s.IsCompleted && !s.IsWarmup && s.Reps > 0)
                .Sum(s => (s.IsBodyweight ? 0 : s.Weight * s.Reps) + s.Drops.Sum(d => d.IsBodyweight ? 0 : d.Weight * d.Reps)))))
            .ToListAsync();
        var recordSince = today.AddDays(-27);
        var exerciseRows = completed.SelectMany(w => w.WorkoutExercises.SelectMany(e => e.SetEntries
            .Where(s => s.IsCompleted && !s.IsWarmup && s.Reps > 0)
            .Select(s => new { w.Date, WorkoutId = w.Id, e.ExerciseId, e.Exercise.Name, s.Weight, s.IsSkipped })));
        var records = daily.Count == 0 ? [] : await exerciseRows.GroupBy(s => new { s.ExerciseId, s.Name })
            .Select(g => new DashboardExercise(g.Key.ExerciseId, g.Key.Name, g.Max(s => s.Weight),
                g.Where(s => s.Date < recordSince).Select(s => s.Weight).DefaultIfEmpty().Max(),
                g.Where(s => s.Date >= recordSince).Select(s => s.Weight).DefaultIfEmpty().Max(),
                g.Where(s => !s.IsSkipped && s.Weight > 0 && s.Date <= until).Max(s => (DateOnly?)s.Date),
                g.Where(s => !s.IsSkipped && s.Weight > 0 && s.Date <= until)
                    .OrderByDescending(s => s.Date).ThenByDescending(s => s.WorkoutId)
                    .Select(s => (int?)s.WorkoutId).FirstOrDefault())).ToListAsync();

        var since = until.AddDays(1 - days);
        var progressSince = until.AddDays(1 - Math.Max(6, chartWeeks) * 7);
        var monday = today.AddDays(-((int)today.DayOfWeek + 6) % 7);
        var week = monday.AddDays(weekOffset * 7);
        var lastId = completed.OrderByDescending(w => w.Date).ThenByDescending(w => w.Id).Select(w => (int?)w.Id).Take(1);
        var sessions = db.WorkoutSessions.AsNoTracking().Where(w => w.UserId == subject.Id);
        var nextId = sessions.Where(w => w.Date >= today && (w.Status == WorkoutStatus.Planned ||
                (w.Status == WorkoutStatus.Legacy && w.CompletedAtUtc == null && w.StartedAtUtc == null &&
                 w.TrainingProgramId == null && !w.WorkoutExercises.Any(e => e.SetEntries.Any(s => s.IsCompleted && !s.IsWarmup && s.Reps > 0)))))
            .OrderBy(w => w.Date).ThenBy(w => w.Id).Select(w => (int?)w.Id).Take(1);
        // Disjoint windows keep an old selected period from loading every intervening year.
        var detail = sessions.Where(w => (w.Date >= since && w.Date <= until) ||
            (w.Date >= today.AddDays(-27) && w.Date <= today) ||
            (w.Date >= progressSince && w.Date <= until) ||
            (w.Date >= week && w.Date < week.AddDays(7)) ||
            (w.Date >= monday && w.Date <= today) ||
            w.Status == WorkoutStatus.InProgress || w.Status == WorkoutStatus.Paused ||
            (w.Status == WorkoutStatus.Legacy && w.CompletedAtUtc == null && (w.StartedAtUtc != null || w.TrainingProgramId != null)) ||
            (w.TrainingProgramId != null && w.ScheduledDate >= today) || lastId.Contains(w.Id) || nextId.Contains(w.Id));
        // Projection avoids transferring JSON snapshots, exercise instructions/images and tracking state.
        // There is one collection chain, so a single query has no sibling collection cross product.
        var workouts = await detail.AsSingleQuery().OrderByDescending(w => w.Date).ThenByDescending(w => w.Id)
            .Select(w => new WorkoutSession
            {
                Id = w.Id, UserId = w.UserId, Title = w.Title, Notes = w.Notes, Date = w.Date,
                Status = w.Status, EntryMode = w.EntryMode, StartedAtUtc = w.StartedAtUtc, CompletedAtUtc = w.CompletedAtUtc,
                CreatedAtUtc = w.CreatedAtUtc, DurationSeconds = w.DurationSeconds, PlannedMinutes = w.PlannedMinutes,
                TrainingProgramId = w.TrainingProgramId, ProgramWorkoutKey = w.ProgramWorkoutKey, ScheduledDate = w.ScheduledDate,
                WorkoutExercises = w.WorkoutExercises.Select(e => new WorkoutExercise
                {
                    Id = e.Id, WorkoutSessionId = e.WorkoutSessionId, ExerciseId = e.ExerciseId, Order = e.Order,
                    Exercise = new Exercise { Id = e.ExerciseId, Name = e.Exercise.Name },
                    SetEntries = e.SetEntries.Select(s => new SetEntry
                    {
                        Id = s.Id, SetNumber = s.SetNumber, Weight = s.Weight, Reps = s.Reps,
                        IsBodyweight = s.IsBodyweight, Drops = s.Drops,
                        IsCompleted = s.IsCompleted, IsWarmup = s.IsWarmup, IsSkipped = s.IsSkipped
                    }).ToList()
                }).ToList()
            }).ToListAsync();
        var measurements = db.BodyMeasurements.AsNoTracking().Where(m => m.UserId == subject.Id && m.Date <= until);
        var lastMeasurements = measurements.OrderByDescending(m => m.Date).ThenByDescending(m => m.Id).Select(m => m.Id).Take(2);
        var data = new DashboardData
        {
            Subject = subject, Today = today, Until = until, Days = days,
            Settings = config.Settings ?? new DashboardSettings { UserId = subject.Id },
            History = new(daily, records), Workouts = workouts,
            Measurements = await measurements.Where(m => m.Date >= until.AddDays(-28) || lastMeasurements.Contains(m.Id))
                .OrderBy(m => m.Date).ThenBy(m => m.Id).ToListAsync(),
            CheckIns = await db.DailyCheckIns.AsNoTracking().Where(c => c.UserId == subject.Id && c.Date >= today.AddDays(-29) && c.Date <= today)
                .OrderBy(c => c.Date).ToListAsync()
        };
        return new(data, config.Layout?.WidgetsJson, config.Layout is null);
    }
}
