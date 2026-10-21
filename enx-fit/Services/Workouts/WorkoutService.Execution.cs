using enx_fit.Models;
using Microsoft.EntityFrameworkCore;

namespace enx_fit.Services.Workouts;

public sealed record WorkoutExecutionState(Guid Revision, string Status, DateTime ServerNowUtc, int ElapsedSeconds,
    DateTime? RestEndsAtUtc, int? RestRemainingSeconds, int? RestAfterSetId, int CompletedSets, int RemainingSets);
public sealed class WorkoutConflictException(string message) : InvalidOperationException(message);

public partial class WorkoutService
{
    public async Task ChangeExerciseRestAsync(int id, int entryId, int seconds, Guid? operationId, Guid? expectedRevision)
    {
        if (await CommandExistsAsync(id, operationId)) return;
        if (seconds is < 0 or > 1800) throw new InvalidOperationException("Отдых: от 0 до 1800 секунд.");
        var session = await EditableSessionAsync(id);
        CheckRevision(session, expectedRevision);
        if (session.State == WorkoutStatus.Paused) throw new InvalidOperationException("Продолжите тренировку перед изменением отдыха.");
        var entry = session.WorkoutExercises.SingleOrDefault(e => e.Id == entryId) ?? throw new KeyNotFoundException();
        entry.TargetRestSeconds = seconds;
        foreach (var set in entry.SetEntries.Where(s => !s.IsCompleted && !s.IsSkipped)) set.RestSeconds = seconds;
        await SaveCommandAsync(session, operationId);
    }

    internal static WorkoutExecutionState State(WorkoutSession session)
    {
        var sets = session.WorkoutExercises.SelectMany(e => e.SetEntries).ToList();
        return new(session.Revision, session.State.ToString(), DateTime.UtcNow,
            WorkoutStates.ElapsedSeconds(session, DateTime.UtcNow), session.RestEndsAtUtc is { } end ? DateTime.SpecifyKind(end, DateTimeKind.Utc) : null,
            session.RestRemainingSeconds, session.RestAfterSetId, sets.Count(s => s.IsCompleted),
            sets.Count(s => !s.IsCompleted && !s.IsSkipped));
    }

    public async Task<WorkoutExecutionState> ExecutionStateAsync(int id) => State(await FindAsync(id) ?? throw new KeyNotFoundException());

    private Task<bool> CommandExistsAsync(int id, Guid? operationId) => operationId.HasValue
        ? dbContext.Set<WorkoutCommand>().AnyAsync(c => c.WorkoutSessionId == id && c.OperationId == operationId &&
            VisibleWorkouts().Any(w => w.Id == id)) : Task.FromResult(false);

    private static void CheckRevision(WorkoutSession session, Guid? expected)
    {
        if (expected.HasValue && expected != session.Revision)
            throw new WorkoutConflictException("Тренировка изменилась в другой вкладке. Обновите страницу; несохранённые изменения останутся на устройстве.");
    }

    private async Task SaveCommandAsync(WorkoutSession session, Guid? operationId)
    {
        session.Revision = Guid.NewGuid();
        if (operationId.HasValue && operationId != Guid.Empty)
            dbContext.Add(new WorkoutCommand { WorkoutSessionId = session.Id, OperationId = operationId.Value });
        try { await dbContext.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            dbContext.ChangeTracker.Clear();
            if (await CommandExistsAsync(session.Id, operationId)) return;
            throw new WorkoutConflictException("Тренировка изменилась в другой вкладке. Обновите страницу; локальные изменения сохранены.");
        }
    }

    public async Task<WorkoutExecutionState> ControlAsync(int id, string action, Guid? operationId = null,
        Guid? expectedRevision = null, int seconds = 0, DateTime? performedAtUtc = null)
    {
        if (await CommandExistsAsync(id, operationId)) return await ExecutionStateAsync(id);
        if (action == "cancel" && (await FindAsync(id))?.State == WorkoutStatus.Cancelled)
            return await ExecutionStateAsync(id);
        var session = await EditableSessionAsync(id);
        CheckRevision(session, expectedRevision);
        if (!session.IsActive) throw new InvalidOperationException("Сначала начните тренировку.");
        var now = DateTime.UtcNow;
        var at = performedAtUtc is { } candidate && candidate <= now && candidate >= session.StartedAtUtc &&
            (!session.PausedAtUtc.HasValue || candidate >= session.PausedAtUtc) ? candidate : now;
        switch (action)
        {
            case "pause" when session.State == WorkoutStatus.Paused:
            case "resume" when session.State == WorkoutStatus.InProgress:
                break;
            case "pause" when session.State == WorkoutStatus.InProgress:
                session.Status = WorkoutStatus.Paused; session.PausedAtUtc = at;
                session.RestRemainingSeconds = session.RestEndsAtUtc is { } end ? Math.Max(0, (int)Math.Ceiling((end - at).TotalSeconds)) : session.RestRemainingSeconds;
                session.RestEndsAtUtc = null;
                break;
            case "resume" when session.State == WorkoutStatus.Paused:
                session.PausedSeconds += Math.Max(0, (int)(at - (session.PausedAtUtc ?? at)).TotalSeconds);
                session.PausedAtUtc = null; session.Status = WorkoutStatus.InProgress;
                session.RestEndsAtUtc = session.RestRemainingSeconds is > 0 ? at.AddSeconds(session.RestRemainingSeconds.Value) : null;
                session.RestRemainingSeconds = null;
                break;
            case "adjust-rest":
                if (seconds is < -300 or > 300) throw new InvalidOperationException("Измените отдых не более чем на 300 секунд.");
                if (session.RestRemainingSeconds.HasValue && session.RestEndsAtUtc is null)
                    session.RestRemainingSeconds = Math.Clamp(session.RestRemainingSeconds.Value + seconds, 0, 1800);
                else if (session.RestEndsAtUtc is { } restEnd)
                    session.RestEndsAtUtc = at.AddSeconds(Math.Clamp((restEnd - at).TotalSeconds + seconds, 0, 1800));
                break;
            case "start-rest" when session.State == WorkoutStatus.InProgress:
                if (seconds is < 1 or > 900) throw new InvalidOperationException("Таймер: от 1 до 900 секунд.");
                session.RestEndsAtUtc = at.AddSeconds(seconds);
                session.RestRemainingSeconds = null;
                session.RestAfterSetId = null;
                break;
            case "pause-rest" when session.State == WorkoutStatus.InProgress:
                session.RestRemainingSeconds = session.RestEndsAtUtc is { } deadline
                    ? Math.Max(0, (int)Math.Ceiling((deadline - at).TotalSeconds)) : session.RestRemainingSeconds;
                session.RestEndsAtUtc = null;
                break;
            case "resume-rest" when session.State == WorkoutStatus.InProgress:
                session.RestEndsAtUtc = session.RestRemainingSeconds is > 0 ? at.AddSeconds(session.RestRemainingSeconds.Value) : null;
                session.RestRemainingSeconds = null;
                break;
            case "set-rest" when session.State == WorkoutStatus.InProgress:
                if (seconds is < 0 or > 900) throw new InvalidOperationException("Таймер: от 0 до 900 секунд.");
                if (session.RestEndsAtUtc.HasValue) session.RestEndsAtUtc = seconds > 0 ? at.AddSeconds(seconds) : null;
                else if (session.RestRemainingSeconds.HasValue) session.RestRemainingSeconds = seconds > 0 ? seconds : null;
                if (seconds == 0) session.RestAfterSetId = null;
                break;
            case "skip-rest":
                session.RestEndsAtUtc = null; session.RestRemainingSeconds = null; session.RestAfterSetId = null;
                break;
            case "cancel":
                session.Status = WorkoutStatus.Cancelled; session.CompletedAtUtc = now;
                // Cancelled rows retain their calendar identity; the filtered slot index permits a retry.
                session.DurationSeconds = WorkoutStates.ElapsedSeconds(session, now);
                session.RestEndsAtUtc = null; session.RestRemainingSeconds = null; session.RestAfterSetId = null;
                break;
            default: throw new InvalidOperationException("Действие недоступно в текущем состоянии тренировки.");
        }
        await SaveCommandAsync(session, operationId);
        return await ExecutionStateAsync(id);
    }
}
