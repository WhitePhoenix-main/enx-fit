using enx_fit.Data;
using enx_fit.Models;
using enx_fit.Security;
using enx_fit.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace enx_fit.Services;

public class WorkoutService(ApplicationDbContext dbContext, CurrentUser currentUser)
{
    public async Task<IReadOnlyList<WorkoutSession>> GetAllAsync(string? ownerId = null)
    {
        var query = VisibleWorkouts();

        if (currentUser.IsAdministrator && !string.IsNullOrWhiteSpace(ownerId))
        {
            query = query.Where(workout => workout.UserId == ownerId);
        }

        return await query
            .AsNoTracking()
            .Include(w => w.WorkoutExercises)
            .OrderByDescending(w => w.Date)
            .ThenByDescending(w => w.Id)
            .ToListAsync();
    }

    public Task<WorkoutSession?> FindAsync(int id) =>
        VisibleWorkouts()
            .AsNoTracking()
            .AsSplitQuery()
            .Include(w => w.WorkoutExercises)
                .ThenInclude(w => w.Exercise)
            .Include(w => w.WorkoutExercises)
                .ThenInclude(w => w.SetEntries)
            .SingleOrDefaultAsync(w => w.Id == id);

    public async Task<IReadOnlyList<Exercise>> GetExercisesAsync(int workoutSessionId) =>
        await dbContext.Exercises
            .AsNoTracking()
            .Where(e => VisibleWorkouts().Any(workout => workout.Id == workoutSessionId))
            .Where(e => !e.WorkoutExercises.Any(w => w.WorkoutSessionId == workoutSessionId))
            .OrderBy(e => e.Name)
            .ToListAsync();

    public async Task<int> CreateAsync(WorkoutSessionInputModel input, WorkoutBuilderInput? builder = null)
    {
        var workout = new WorkoutSession
        {
            UserId = currentUser.ResolveOwnerId(input.OwnerId)
        };
        ApplyInput(workout, input);
        if (builder is not null)
        {
            workout.BuilderConfigurationJson = System.Text.Json.JsonSerializer.Serialize(builder, WorkoutBuilderInput.JsonOptions);
            var order = 0;
            foreach (var block in builder.Blocks)
            foreach (var item in block.Exercises)
            {
                workout.WorkoutExercises.Add(new WorkoutExercise
                {
                    ExerciseId = item.ExerciseId, Order = ++order,
                    Notes = block.Name, TargetSets = item.Sets.Count,
                    TargetRepsMin = item.Sets.Min(s => s.Reps), TargetRepsMax = item.Sets.Max(s => s.Reps),
                    TargetWeightKg = item.Sets.Max(s => s.Weight)
                });
            }
        }
        dbContext.WorkoutSessions.Add(workout);
        await dbContext.SaveChangesAsync();
        return workout.Id;
    }

    public async Task<bool> UpdateAsync(int id, WorkoutSessionInputModel input)
    {
        var workout = await VisibleWorkouts().SingleOrDefaultAsync(candidate => candidate.Id == id);

        if (workout is null)
        {
            return false;
        }

        ApplyInput(workout, input);
        if (currentUser.IsAdministrator && !string.IsNullOrWhiteSpace(input.OwnerId))
        {
            if (workout.UserId != input.OwnerId)
            {
                // A transferred diary entry must not occupy the former owner's program slot.
                workout.TrainingProgramId = null; workout.ProgramWorkoutKey = null; workout.ScheduledDate = null;
            }
            workout.UserId = input.OwnerId;
        }
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var workout = await VisibleWorkouts().SingleOrDefaultAsync(candidate => candidate.Id == id);

        if (workout is null)
        {
            return false;
        }

        dbContext.WorkoutSessions.Remove(workout);
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<CompleteWorkoutResult> CompleteAsync(int id)
    {
        var workout = await VisibleWorkouts().AsSplitQuery().Include(w => w.WorkoutExercises).ThenInclude(e => e.SetEntries)
            .SingleOrDefaultAsync(w => w.Id == id);
        if (workout is null) return CompleteWorkoutResult.NotFound;
        if (!workout.WorkoutExercises.Any(e => e.SetEntries.Any(s => !s.IsWarmup && s.Reps > 0))) return CompleteWorkoutResult.Empty;
        workout.CompletedAtUtc ??= DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
        return CompleteWorkoutResult.Success;
    }

    public async Task<AddWorkoutExerciseResult> AddExerciseAsync(int workoutSessionId, int exerciseId)
    {
        if (!await VisibleWorkouts().AnyAsync(w => w.Id == workoutSessionId))
        {
            return AddWorkoutExerciseResult.WorkoutNotFound;
        }

        if (!await dbContext.Exercises.AnyAsync(e => e.Id == exerciseId))
        {
            return AddWorkoutExerciseResult.ExerciseNotFound;
        }

        if (await dbContext.WorkoutExercises.AnyAsync(w =>
                w.WorkoutSessionId == workoutSessionId && w.ExerciseId == exerciseId))
        {
            return AddWorkoutExerciseResult.AlreadyAdded;
        }

        var nextOrder = await dbContext.WorkoutExercises
            .Where(w => w.WorkoutSessionId == workoutSessionId)
            .Select(w => (int?)w.Order)
            .MaxAsync() ?? 0;

        dbContext.WorkoutExercises.Add(new WorkoutExercise
        {
            WorkoutSessionId = workoutSessionId,
            ExerciseId = exerciseId,
            Order = nextOrder + 1
        });

        await dbContext.SaveChangesAsync();
        return AddWorkoutExerciseResult.Success;
    }

    public async Task<AddSetEntryResult> AddSetAsync(int workoutSessionId, AddSetEntryInputModel input)
    {
        var workoutExerciseExists = await dbContext.WorkoutExercises
            .AnyAsync(w =>
                w.Id == input.WorkoutExerciseId &&
                w.WorkoutSessionId == workoutSessionId &&
                VisibleWorkouts().Any(workout => workout.Id == w.WorkoutSessionId));

        if (!workoutExerciseExists)
        {
            return AddSetEntryResult.WorkoutExerciseNotFound;
        }

        dbContext.SetEntries.Add(new SetEntry
        {
            WorkoutExerciseId = input.WorkoutExerciseId,
            SetNumber = input.SetNumber,
            Weight = input.Weight,
            Reps = input.Reps,
            Rir = input.Rir,
            IsWarmup = input.IsWarmup,
            Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim()
        });

        await dbContext.SaveChangesAsync();
        return AddSetEntryResult.Success;
    }

    private static void ApplyInput(WorkoutSession workout, WorkoutSessionInputModel input)
    {
        workout.Date = input.Date;
        workout.Title = string.IsNullOrWhiteSpace(input.Title) ? null : input.Title.Trim();
        workout.Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim();
    }

    private IQueryable<WorkoutSession> VisibleWorkouts() =>
        currentUser.IsAdministrator
            ? dbContext.WorkoutSessions
            : dbContext.WorkoutSessions.Where(workout => workout.UserId == currentUser.Id);
}

public enum AddWorkoutExerciseResult
{
    Success,
    WorkoutNotFound,
    ExerciseNotFound,
    AlreadyAdded
}

public enum AddSetEntryResult
{
    Success,
    WorkoutExerciseNotFound
}

public enum CompleteWorkoutResult { Success, NotFound, Empty }
