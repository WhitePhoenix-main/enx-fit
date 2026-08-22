using enx_fit.Data;
using enx_fit.Models;
using Microsoft.EntityFrameworkCore;

namespace enx_fit.Services;

public class AnalyticsDataService(ApplicationDbContext dbContext)
{
    public async Task<IReadOnlyList<Exercise>> GetExercisesAsync() =>
        await dbContext.Exercises
            .AsNoTracking()
            .OrderBy(exercise => exercise.Name)
            .ToListAsync();

    public async Task<IReadOnlyList<WorkoutSession>> GetWorkoutsForExerciseAsync(int exerciseId) =>
        await dbContext.WorkoutSessions
            .AsNoTracking()
            .AsSplitQuery()
            .Where(workout => workout.WorkoutExercises.Any(exercise => exercise.ExerciseId == exerciseId))
            .Include(workout => workout.WorkoutExercises)
                .ThenInclude(exercise => exercise.SetEntries)
            .OrderBy(workout => workout.Date)
            .ThenBy(workout => workout.Id)
            .ToListAsync();
}
