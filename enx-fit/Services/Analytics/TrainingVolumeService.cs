using enx_fit.Models;

namespace enx_fit.Services.Analytics;

public sealed record MuscleVolume(string MuscleGroup, int WorkingSets, decimal VolumeLoad);
public interface ITrainingVolumeService
{
    IReadOnlyList<MuscleVolume> Calculate(IEnumerable<WorkoutSession> sessions);
}
public sealed class TrainingVolumeService : ITrainingVolumeService
{
    public IReadOnlyList<MuscleVolume> Calculate(IEnumerable<WorkoutSession> sessions) => sessions
        .Where(s => s.State == WorkoutStatus.Completed).SelectMany(s => s.WorkoutExercises)
        .GroupBy(e => string.IsNullOrWhiteSpace(e.Exercise.MuscleGroup) ? "Группа не указана" : e.Exercise.MuscleGroup)
        .Select(g => new MuscleVolume(g.Key, g.Sum(e => e.SetEntries.Count(s => s.IsCompleted && !s.IsWarmup && s.Reps > 0)),
            g.SelectMany(e => e.SetEntries).Where(s => s.IsCompleted && !s.IsWarmup && s.Reps > 0).Sum(s => s.Weight * s.Reps))).ToList();
}
