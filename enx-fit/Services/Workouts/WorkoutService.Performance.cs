using enx_fit.ViewModels.Workouts;
using Microsoft.EntityFrameworkCore;

namespace enx_fit.Services.Workouts;

public partial class WorkoutService
{
    public async Task<IReadOnlyList<ExercisePerformance>> PerformanceAsync(Guid? exerciseId = null, int? beforeWorkoutId = null)
    {
        // The cutoff also resolves the owner: an administrator must never compare two diaries.
        var before = beforeWorkoutId.HasValue ? await FindAsync(beforeWorkoutId.Value) : null;
        if (beforeWorkoutId.HasValue && before is null) throw new KeyNotFoundException();
        var owner = before?.UserId ?? currentUser.Id;
        var sessions = VisibleWorkouts().AsNoTracking().Where(WorkoutStates.Completed)
            .Where(w => w.UserId == owner && w.Date <= currentUser.LocalToday);
        if (before is not null)
            sessions = sessions.Where(w => w.Date < before.Date || w.Date == before.Date && w.Id < before.Id);
        var rows = await dbContext.SetEntries.AsNoTracking()
            .Where(s => s.IsCompleted && !s.IsSkipped && !s.IsWarmup && s.Reps > 0 &&
                sessions.Any(w => w.Id == s.WorkoutExercise.WorkoutSessionId) &&
                (!exerciseId.HasValue || s.WorkoutExercise.ExerciseId == exerciseId))
            .Select(s => new { s.SetNumber, s.Weight, s.Reps, s.IsBodyweight,
                Exercise = s.WorkoutExercise.Exercise, WorkoutId = s.WorkoutExercise.WorkoutSessionId,
                s.WorkoutExercise.WorkoutSession.Date, s.WorkoutExercise.WorkoutSession.Title,
                Drops = s.Drops.OrderBy(d => d.Order).Select(d => new { d.Weight, d.Reps, d.IsBodyweight }).ToList() })
            .ToListAsync();
        return rows.GroupBy(s => s.Exercise.Id).Select(g => new ExercisePerformance(g.First().Exercise,
            g.GroupBy(s => s.WorkoutId).Select(h => new ExerciseHistorySession(h.Key, h.First().Date,
                h.First().Title ?? "Тренировка", h.OrderBy(s => s.SetNumber).Select(s => new PerformanceSet(
                    s.SetNumber, s.Weight, s.Reps, s.IsBodyweight,
                    (s.IsBodyweight ? 0 : s.Weight * s.Reps) + s.Drops.Sum(d => d.IsBodyweight ? 0 : d.Weight * d.Reps),
                    s.Drops.Select(d => new PerformanceDrop(d.Weight, d.Reps, d.IsBodyweight)).ToArray())).ToArray()))
                .OrderByDescending(h => h.Date).ThenByDescending(h => h.WorkoutId).ToArray()))
            .OrderByDescending(e => e.History[0].Date).ToArray();
    }
}
