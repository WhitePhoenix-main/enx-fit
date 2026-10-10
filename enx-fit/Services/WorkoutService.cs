using enx_fit.Data;
using enx_fit.Models;
using enx_fit.Security;
using enx_fit.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace enx_fit.Services;

public partial class WorkoutService(ApplicationDbContext dbContext, CurrentUser currentUser)
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

    public async Task<int> CreateAsync(WorkoutSessionInputModel input, WorkoutBuilderInput? builder = null)
    {
        var workout = new WorkoutSession
        {
            UserId = currentUser.ResolveOwnerId(input.OwnerId), Status = WorkoutStatus.Planned
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
                    Notes = block.Name, BlockKind = block.Kind, TargetRestSeconds = item.Sets.FirstOrDefault()?.RestSeconds, TargetSets = item.Sets.Count,
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
        workout.Revision = Guid.NewGuid();
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

    public async Task<CompleteWorkoutResult> CompleteAsync(int id, Guid? operationId = null, Guid? expectedRevision = null)
    {
        if (await CommandExistsAsync(id, operationId)) return CompleteWorkoutResult.Success;
        var workout = await VisibleWorkouts().AsSplitQuery().Include(w => w.WorkoutExercises).ThenInclude(e => e.SetEntries)
            .SingleOrDefaultAsync(w => w.Id == id);
        if (workout is null) return CompleteWorkoutResult.NotFound;
        if (workout.State == WorkoutStatus.Cancelled) return CompleteWorkoutResult.NotFound;
        if (workout.Status == WorkoutStatus.Completed) return CompleteWorkoutResult.Success;
        CheckRevision(workout, expectedRevision);
        if (!workout.WorkoutExercises.Any(e => e.SetEntries.Any(s => s.IsCompleted && !s.IsWarmup && s.Reps > 0))) return CompleteWorkoutResult.Empty;
        if (workout.CompletedAtUtc is null && workout.SourceStructureJson is not null)
            workout.TemplateDecisionPending = !StructureMatches(workout);
        workout.DurationSeconds = workout.StartedAtUtc.HasValue ? WorkoutStates.ElapsedSeconds(workout, DateTime.UtcNow) : workout.DurationSeconds;
        workout.CompletedAtUtc ??= DateTime.UtcNow;
        workout.Status = WorkoutStatus.Completed;
        workout.RestEndsAtUtc = null; workout.RestRemainingSeconds = null; workout.RestAfterSetId = null;
        foreach (var set in workout.WorkoutExercises.SelectMany(e => e.SetEntries).Where(s => !s.IsCompleted)) set.IsSkipped = true;
        await SaveCommandAsync(workout, operationId);
        return CompleteWorkoutResult.Success;
    }

    public async Task<AddWorkoutExerciseResult> AddExerciseAsync(int workoutSessionId, Guid exerciseId)
    {
        if (await VisibleWorkouts().AnyAsync(w => w.Id == workoutSessionId && (w.Status == WorkoutStatus.Completed || w.Status == WorkoutStatus.Cancelled || (w.StartedAtUtc != null && w.CompletedAtUtc != null))))
            return AddWorkoutExerciseResult.WorkoutNotFound;
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
        var session = await VisibleWorkouts().SingleAsync(w => w.Id == workoutSessionId);
        await SaveCommandAsync(session, null);
        return AddWorkoutExerciseResult.Success;
    }

    public async Task<AddSetEntryResult> AddSetAsync(int workoutSessionId, AddSetEntryInputModel input, bool completed = true)
    {
        if (await VisibleWorkouts().AnyAsync(w => w.Id == workoutSessionId && (w.Status == WorkoutStatus.Completed || w.Status == WorkoutStatus.Cancelled || (w.StartedAtUtc != null && w.CompletedAtUtc != null))))
            return AddSetEntryResult.WorkoutExerciseNotFound;
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
            IsCompleted = completed, RestSeconds = input.RestSeconds,
            Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim()
        });
        var session = await VisibleWorkouts().SingleAsync(w => w.Id == workoutSessionId);
        await SaveCommandAsync(session, null);
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
