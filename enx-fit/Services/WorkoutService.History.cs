using enx_fit.Models;
using enx_fit.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace enx_fit.Services;

public partial class WorkoutService
{
    // A comparison is the owner's latest earlier completed session for this exercise.
    // It never fills the current session's actual values or effort.
    public async Task<IReadOnlyDictionary<Guid, PreviousExerciseResult>> PreviousExerciseResultsAsync(WorkoutSession workout)
    {
        if (!await VisibleWorkouts().AnyAsync(w => w.Id == workout.Id)) return new Dictionary<Guid, PreviousExerciseResult>();
        var exerciseIds = workout.WorkoutExercises.Select(e => e.ExerciseId).Distinct().ToArray();
        if (exerciseIds.Length == 0) return new Dictionary<Guid, PreviousExerciseResult>();
        var earlier = VisibleWorkouts().Where(w => w.UserId == workout.UserId && w.Date <= currentUser.LocalToday &&
            (w.Date < workout.Date || (w.Date == workout.Date && w.Id < workout.Id))).Where(WorkoutStates.Completed);
        var latestIds = await dbContext.WorkoutExercises.AsNoTracking()
            .Where(e => exerciseIds.Contains(e.ExerciseId) && earlier.Any(w => w.Id == e.WorkoutSessionId) &&
                e.SetEntries.Any(s => s.IsCompleted && !s.IsSkipped && !s.IsWarmup && s.Reps > 0))
            .GroupBy(e => e.ExerciseId)
            .Select(g => g.OrderByDescending(e => e.WorkoutSession.Date).ThenByDescending(e => e.WorkoutSessionId).ThenByDescending(e => e.Id).First().Id)
            .ToListAsync();
        if (latestIds.Count == 0) return new Dictionary<Guid, PreviousExerciseResult>();
        var results = await dbContext.WorkoutExercises.AsNoTracking().Where(e => latestIds.Contains(e.Id))
            .Select(e => new
            {
                e.ExerciseId, e.WorkoutSessionId, e.WorkoutSession.Date, e.WorkoutSession.Title,
                Sets = e.SetEntries.Where(s => s.IsCompleted && !s.IsSkipped && !s.IsWarmup && s.Reps > 0)
                    .OrderBy(s => s.SetNumber).ThenBy(s => s.Id)
                    .Select(s => new PreviousWorkingSet(s.SetNumber, s.Weight, s.Reps, s.Rir)).ToList()
            }).ToListAsync();
        return results.ToDictionary(e => e.ExerciseId,
            e => new PreviousExerciseResult(e.WorkoutSessionId, e.Date, e.Title ?? "Прошлая тренировка", e.Sets));
    }
}
