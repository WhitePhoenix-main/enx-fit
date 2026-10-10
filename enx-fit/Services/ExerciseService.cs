using enx_fit.Data;
using enx_fit.Extensions;
using enx_fit.Models;
using enx_fit.Security;
using enx_fit.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace enx_fit.Services;

public class ExerciseService(ApplicationDbContext dbContext, CurrentUser currentUser)
{
    public async Task<IReadOnlyList<Exercise>> GetAllAsync(string? muscleGroup)
    {
        var query = dbContext.Exercises.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(muscleGroup))
        {
            query = query.Where(e => e.MuscleGroup == muscleGroup);
        }

        var exercises = await query.ToListAsync();
        return exercises.OrderBy(ExercisePresentation.DisplayName,
            StringComparer.Create(System.Globalization.CultureInfo.GetCultureInfo("ru-RU"), true)).ToList();
    }

    public async Task<IReadOnlyList<Guid>> GetRecentIdsAsync(string ownerId)
    {
        var sessions = dbContext.WorkoutSessions.AsNoTracking()
            .Where(w => w.UserId == ownerId && w.Date <= currentUser.LocalToday).Where(WorkoutStates.Completed)
            .Where(w => w.WorkoutExercises.Any(e => e.SetEntries.Any(s => s.IsCompleted && !s.IsSkipped && !s.IsWarmup && s.Reps > 0)))
            .OrderByDescending(w => w.Date).ThenByDescending(w => w.Id).Take(5);
        var entries = await dbContext.WorkoutExercises.AsNoTracking()
            .Where(e => sessions.Any(w => w.Id == e.WorkoutSessionId) &&
                e.SetEntries.Any(s => s.IsCompleted && !s.IsSkipped && !s.IsWarmup && s.Reps > 0))
            .OrderByDescending(e => e.WorkoutSession.Date).ThenByDescending(e => e.WorkoutSessionId).ThenBy(e => e.Order).ThenBy(e => e.Id)
            .Select(e => e.ExerciseId).ToListAsync();
        return entries.Distinct().ToArray();
    }

    public async Task<IReadOnlyList<string>> GetMuscleGroupsAsync() =>
        await dbContext.Exercises
            .AsNoTracking()
            .Select(e => e.MuscleGroup)
            .Distinct()
            .OrderBy(muscleGroup => muscleGroup)
            .ToListAsync();

    public Task<Exercise?> FindAsync(Guid id) =>
        dbContext.Exercises
            .AsNoTracking()
            .SingleOrDefaultAsync(e => e.Id == id);

    public async Task<ExerciseWriteResult> CreateAsync(ExerciseInputModel input)
    {
        var name = input.Name.Trim();

        if (await NameExistsAsync(name))
        {
            return ExerciseWriteResult.DuplicateName;
        }

        var exercise = new Exercise();
        ApplyInput(exercise, input);
        dbContext.Exercises.Add(exercise);

        return await SaveAsync();
    }

    public async Task<ExerciseWriteResult> UpdateAsync(Guid id, ExerciseInputModel input)
    {
        var exercise = await dbContext.Exercises.FindAsync(id);

        if (exercise is null)
        {
            return ExerciseWriteResult.NotFound;
        }

        var name = input.Name.Trim();

        if (await NameExistsAsync(name, id))
        {
            return ExerciseWriteResult.DuplicateName;
        }

        ApplyInput(exercise, input);
        return await SaveAsync();
    }

    public async Task<ExerciseDeleteResult> DeleteAsync(Guid id)
    {
        var exercise = await dbContext.Exercises.FindAsync(id);

        if (exercise is null)
        {
            return ExerciseDeleteResult.NotFound;
        }

        dbContext.Exercises.Remove(exercise);

        try
        {
            await dbContext.SaveChangesAsync();
            return ExerciseDeleteResult.Success;
        }
        catch (DbUpdateException)
        {
            return ExerciseDeleteResult.InUse;
        }
    }

    private Task<bool> NameExistsAsync(string name, Guid? excludedId = null) =>
        dbContext.Exercises.AnyAsync(e =>
            e.Name.ToLower() == name.ToLower() &&
            (!excludedId.HasValue || e.Id != excludedId.Value));

    private async Task<ExerciseWriteResult> SaveAsync()
    {
        try
        {
            await dbContext.SaveChangesAsync();
            return ExerciseWriteResult.Success;
        }
        catch (DbUpdateException)
        {
            return ExerciseWriteResult.DuplicateName;
        }
    }

    private static void ApplyInput(Exercise exercise, ExerciseInputModel input)
    {
        exercise.Name = input.Name.Trim();
        exercise.MuscleGroup = input.MuscleGroup.Trim();
        exercise.Equipment = input.Equipment.Trim();
        exercise.Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim();
    }
}

public enum ExerciseWriteResult
{
    Success,
    NotFound,
    DuplicateName
}

public enum ExerciseDeleteResult
{
    Success,
    NotFound,
    InUse
}
