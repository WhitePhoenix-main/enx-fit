using enx_fit.Models;

namespace enx_fit.Services;

public class TrainingAnalyticsService
{
    public decimal CalculateTotalWorkoutVolume(WorkoutSession workout) =>
        GetValidSets(workout.WorkoutExercises)
            .Sum(CalculateSetVolume);

    public decimal CalculateEstimatedOneRepMax(decimal weight, int reps)
    {
        if (weight <= 0 || reps <= 0)
        {
            return 0;
        }

        return Math.Round(weight * (1 + reps / 30m), 2);
    }

    public BestExerciseSet? GetBestSetForExercise(IEnumerable<WorkoutSession> workouts, int exerciseId) =>
        GetExerciseSets(workouts, exerciseId)
            .Select(item => new BestExerciseSet(
                item.Workout.Date,
                item.Set.SetNumber,
                item.Set.Weight,
                item.Set.Reps,
                CalculateEstimatedOneRepMax(item.Set.Weight, item.Set.Reps)))
            .OrderByDescending(item => item.EstimatedOneRepMax)
            .ThenByDescending(item => item.Weight)
            .ThenByDescending(item => item.Reps)
            .FirstOrDefault();

    public IReadOnlyList<ExerciseWorkingWeightPoint> GetWorkingWeightTrend(
        IEnumerable<WorkoutSession> workouts,
        int exerciseId) =>
        GetExerciseSets(workouts, exerciseId)
            .GroupBy(item => new { item.Workout.Id, item.Workout.Date })
            .Select(group => new ExerciseWorkingWeightPoint(
                group.Key.Id,
                group.Key.Date,
                group.Max(item => item.Set.Weight)))
            .OrderBy(point => point.Date)
            .ThenBy(point => point.WorkoutSessionId)
            .ToList();

    public IReadOnlyList<ExerciseEstimatedOneRepMaxPoint> GetEstimatedOneRepMaxTrend(
        IEnumerable<WorkoutSession> workouts,
        int exerciseId) =>
        GetExerciseSets(workouts, exerciseId)
            .GroupBy(item => new { item.Workout.Id, item.Workout.Date })
            .Select(group => new ExerciseEstimatedOneRepMaxPoint(
                group.Key.Id,
                group.Key.Date,
                group.Max(item => CalculateEstimatedOneRepMax(item.Set.Weight, item.Set.Reps))))
            .OrderBy(point => point.Date)
            .ThenBy(point => point.WorkoutSessionId)
            .ToList();

    public IReadOnlyList<ExerciseVolumePoint> GetExerciseVolumeTrend(
        IEnumerable<WorkoutSession> workouts,
        int exerciseId) =>
        workouts
            .Select(workout => new ExerciseVolumePoint(
                workout.Id,
                workout.Date,
                workout.WorkoutExercises
                    .Where(exercise => exercise.ExerciseId == exerciseId)
                    .SelectMany(exercise => exercise.SetEntries)
                    .Where(IsValidSet)
                    .Sum(CalculateSetVolume)))
            .Where(point => point.Volume > 0)
            .OrderBy(point => point.Date)
            .ThenBy(point => point.WorkoutSessionId)
            .ToList();

    public IReadOnlyList<WorkoutVolumePoint> GetWorkoutVolumeTrend(IEnumerable<WorkoutSession> workouts) =>
        workouts
            .Select(workout => new WorkoutVolumePoint(
                workout.Id,
                workout.Date,
                CalculateTotalWorkoutVolume(workout)))
            .OrderBy(point => point.Date)
            .ThenBy(point => point.WorkoutSessionId)
            .ToList();

    public string GetExerciseProgressText(IEnumerable<WorkoutSession> workouts, int exerciseId)
    {
        var trend = GetWorkingWeightTrend(workouts, exerciseId);

        if (trend.Count < 2)
        {
            return "Not enough workouts to evaluate progress yet.";
        }

        var change = trend[^1].Weight - trend[0].Weight;

        if (change > 0)
        {
            return $"Working weight increased by {change:0.##} kg.";
        }

        if (change < 0)
        {
            return $"Working weight decreased by {Math.Abs(change):0.##} kg.";
        }

        return "Working weight is stable.";
    }

    private static decimal CalculateSetVolume(SetEntry set) =>
        set.Weight * set.Reps;

    private static IEnumerable<SetEntry> GetValidSets(IEnumerable<WorkoutExercise> workoutExercises) =>
        workoutExercises
            .SelectMany(exercise => exercise.SetEntries)
            .Where(IsValidSet);

    private static IEnumerable<(WorkoutSession Workout, SetEntry Set)> GetExerciseSets(
        IEnumerable<WorkoutSession> workouts,
        int exerciseId) =>
        workouts
            .SelectMany(workout => workout.WorkoutExercises
                .Where(exercise => exercise.ExerciseId == exerciseId)
                .SelectMany(exercise => exercise.SetEntries
                    .Where(IsValidWorkingSet)
                    .Select(set => (workout, set))));

    private static bool IsValidWorkingSet(SetEntry set) =>
        !set.IsWarmup &&
        IsValidSet(set);

    private static bool IsValidSet(SetEntry set) =>
        set.Weight > 0 &&
        set.Reps > 0;
}

public record BestExerciseSet(
    DateOnly Date,
    int SetNumber,
    decimal Weight,
    int Reps,
    decimal EstimatedOneRepMax);

public record ExerciseWorkingWeightPoint(
    int WorkoutSessionId,
    DateOnly Date,
    decimal Weight);

public record ExerciseEstimatedOneRepMaxPoint(
    int WorkoutSessionId,
    DateOnly Date,
    decimal EstimatedOneRepMax);

public record ExerciseVolumePoint(
    int WorkoutSessionId,
    DateOnly Date,
    decimal Volume);

public record WorkoutVolumePoint(
    int WorkoutSessionId,
    DateOnly Date,
    decimal Volume);
