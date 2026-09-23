using enx_fit.Models;

namespace enx_fit.Services;

public sealed record ProgramCompletion(int Planned, int Done, int Due, int DoneDue, int Missed, DateOnly? Ends)
{
    public int Percent => Planned == 0 ? 0 : Math.Min(100, Done * 100 / Planned);
    public int? AdherencePercent => Due == 0 ? null : Math.Min(100, DoneDue * 100 / Due);
}

public interface IProgramAnalysisService
{
    ProgramCompletion Completion(TrainingProgram p, IReadOnlyList<WorkoutSession> sessions, DateOnly today);
}

// Factual completion is Free. Interpretation can be added without changing these metrics.
public sealed class ProgramAnalysisService : IProgramAnalysisService
{
    public ProgramCompletion Completion(TrainingProgram p, IReadOnlyList<WorkoutSession> sessions, DateOnly today)
    {
        var schedule = ProgramSchedule.Occurrences(p).ToList();
        bool Done(ProgramOccurrence o) => sessions.Any(s => s.ProgramWorkoutKey == o.Workout.Key && s.ScheduledDate == o.Date && s.CompletedAtUtc.HasValue);
        var due = schedule.Where(o => o.Date <= today).ToList();
        return new(schedule.Count, schedule.Count(Done), due.Count, due.Count(Done), schedule.Count(o => o.Date < today && !Done(o)),
            p.StartDate?.AddDays(p.Weeks * 7 - 1));
    }
}

public sealed record MuscleVolume(string MuscleGroup, int WorkingSets, decimal VolumeLoad);
public interface ITrainingVolumeService
{
    IReadOnlyList<MuscleVolume> Calculate(IEnumerable<WorkoutSession> sessions);
}
public sealed class TrainingVolumeService : ITrainingVolumeService
{
    public IReadOnlyList<MuscleVolume> Calculate(IEnumerable<WorkoutSession> sessions) => sessions
        .Where(s => s.CompletedAtUtc.HasValue).SelectMany(s => s.WorkoutExercises)
        .GroupBy(e => string.IsNullOrWhiteSpace(e.Exercise.MuscleGroup) ? "Группа не указана" : e.Exercise.MuscleGroup)
        .Select(g => new MuscleVolume(g.Key, g.Sum(e => e.SetEntries.Count(s => !s.IsWarmup && s.Reps > 0)),
            g.SelectMany(e => e.SetEntries).Where(s => !s.IsWarmup && s.Reps > 0).Sum(s => s.Weight * s.Reps))).ToList();
}
