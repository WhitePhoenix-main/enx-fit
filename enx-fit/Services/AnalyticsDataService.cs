using enx_fit.Data;
using enx_fit.Models;
using enx_fit.Security;
using Microsoft.EntityFrameworkCore;

namespace enx_fit.Services;

public class AnalyticsDataService(ApplicationDbContext dbContext, CurrentUser currentUser)
{
    public async Task<IReadOnlyList<Exercise>> GetExercisesAsync() =>
        await dbContext.Exercises
            .AsNoTracking()
            .OrderBy(exercise => exercise.Name)
            .ToListAsync();

    public async Task<IReadOnlyList<WorkoutSession>> GetWorkoutsForExerciseAsync(
        int exerciseId,
        string? ownerId = null)
    {
        var workouts = currentUser.IsAdministrator
            ? dbContext.WorkoutSessions.AsQueryable()
            : dbContext.WorkoutSessions.Where(workout => workout.UserId == currentUser.Id);

        if (currentUser.IsAdministrator)
        {
            if (string.IsNullOrWhiteSpace(ownerId))
            {
                return [];
            }

            workouts = workouts.Where(workout => workout.UserId == ownerId);
        }

        return await workouts
            .AsNoTracking()
            .AsSplitQuery()
            .Where(workout => workout.WorkoutExercises.Any(exercise => exercise.ExerciseId == exerciseId))
            .Include(workout => workout.WorkoutExercises)
                .ThenInclude(exercise => exercise.SetEntries)
            .OrderBy(workout => workout.Date)
            .ThenBy(workout => workout.Id)
            .ToListAsync();
    }
}
