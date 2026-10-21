using enx_fit.Models;

namespace enx_fit.Services.Workouts;

public partial class WorkoutService
{
    public async Task ClearExercisesAsync(int id, Guid? operationId, Guid? expectedRevision)
    {
        if (await CommandExistsAsync(id, operationId)) return;
        var session = await EditableSessionAsync(id);
        CheckRevision(session, expectedRevision);
        if (session.State != WorkoutStatus.InProgress) throw new InvalidOperationException("Продолжите тренировку перед очисткой.");
        dbContext.WorkoutExercises.RemoveRange(session.WorkoutExercises);
        session.WorkoutExercises.Clear();
        session.RestAfterSetId = null;
        session.RestEndsAtUtc = null;
        session.RestRemainingSeconds = null;
        await SaveCommandAsync(session, operationId);
    }

    public async Task SetSessionRestAsync(int id, int seconds, Guid? operationId, Guid? expectedRevision)
    {
        if (await CommandExistsAsync(id, operationId)) return;
        if (seconds is < 0 or > 900) throw new InvalidOperationException("Отдых: от 0 до 900 секунд.");
        var session = await EditableSessionAsync(id);
        CheckRevision(session, expectedRevision);
        if (session.State != WorkoutStatus.InProgress) throw new InvalidOperationException("Продолжите тренировку перед настройкой отдыха.");
        foreach (var exercise in session.WorkoutExercises)
        {
            exercise.TargetRestSeconds = seconds;
            foreach (var set in exercise.SetEntries.Where(s => !s.IsCompleted && !s.IsSkipped)) set.RestSeconds = seconds;
        }
        await SaveCommandAsync(session, operationId);
    }
}
