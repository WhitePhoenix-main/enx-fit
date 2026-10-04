using System.Linq.Expressions;
using enx_fit.Models;

namespace enx_fit.Services;

public static class WorkoutStates
{
    public static readonly Expression<Func<WorkoutSession, bool>> Active = w =>
        w.Status == WorkoutStatus.InProgress || w.Status == WorkoutStatus.Paused ||
        (w.Status == WorkoutStatus.Legacy && w.CompletedAtUtc == null &&
            (w.StartedAtUtc != null || w.TrainingProgramId != null));
    public static readonly Expression<Func<WorkoutSession, bool>> Completed = w =>
        w.Status == WorkoutStatus.Completed || (w.Status == WorkoutStatus.Legacy &&
            (w.CompletedAtUtc != null || (w.StartedAtUtc == null && w.TrainingProgramId == null &&
                w.WorkoutExercises.Any(e => e.SetEntries.Any(s => s.IsCompleted && !s.IsWarmup && s.Reps > 0)))));

    public static int ElapsedSeconds(WorkoutSession session, DateTime now) => session.DurationSeconds ??
        (session.StartedAtUtc is { } start ? Math.Max(0, (int)((session.PausedAtUtc ?? session.CompletedAtUtc ?? now) - start).TotalSeconds - session.PausedSeconds) : 0);
}
