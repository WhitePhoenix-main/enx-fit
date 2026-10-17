using enx_fit.Models;
using enx_fit.Services.Workouts;

namespace enx_fit.ViewModels.Workouts;

public sealed record WorkoutExerciseViewModel(
    WorkoutExercise Exercise,
    WorkoutExerciseBlock Block,
    int Number,
    bool Locked,
    Guid Revision,
    bool CanMoveUp,
    bool CanMoveDown,
    WorkoutBuilderExercise? Blueprint,
    PreviousExerciseResult? PreviousResult,
    IReadOnlyList<WorkoutSetViewModel> Sets,
    int NextSetNumber,
    decimal NextWeight,
    int NextReps)
{
    public static IReadOnlyList<WorkoutExerciseViewModel> CreateAll(
        WorkoutSession workout,
        WorkoutBuilderInput? blueprint,
        IReadOnlyDictionary<Guid, PreviousExerciseResult> previousResults,
        Guid revision)
    {
        var blocks = WorkoutExerciseBlocks.Describe(workout);
        var exercises = workout.WorkoutExercises.OrderBy(e => e.Order).ThenBy(e => e.Id).ToArray();
        var prescriptions = blueprint?.Blocks.SelectMany(b => b.Exercises).ToDictionary(e => e.ExerciseId);
        var result = new List<WorkoutExerciseViewModel>(exercises.Length);

        for (var index = 0; index < exercises.Length; index++)
        {
            var exercise = exercises[index];
            var block = blocks[exercise.Id];
            var sets = exercise.SetEntries.OrderBy(s => s.SetNumber).ThenBy(s => s.Id).ToArray();
            var nextNumber = sets.Length == 0 ? 1 : sets[^1].SetNumber + 1;
            var prescription = prescriptions?.GetValueOrDefault(exercise.ExerciseId);
            var planned = prescription?.Sets.ElementAtOrDefault(nextNumber - 1);

            result.Add(new(
                exercise, block, index + 1, workout.IsFinished, revision,
                index > 0 && blocks[exercises[index - 1].Id].Key == block.Key,
                index < exercises.Length - 1 && blocks[exercises[index + 1].Id].Key == block.Key,
                prescription, previousResults.GetValueOrDefault(exercise.ExerciseId),
                sets.Select(set => new WorkoutSetViewModel(
                    set, exercise, workout.IsFinished,
                    prescription?.Sets.ElementAtOrDefault(set.SetNumber - 1),
                    prescription is null && exercise.TargetSets is { } target && set.SetNumber <= target)).ToArray(),
                nextNumber,
                planned?.Weight ?? sets.LastOrDefault()?.Weight ?? exercise.TargetWeightKg ?? 0,
                planned?.Reps ?? sets.LastOrDefault()?.Reps ?? exercise.TargetRepsMin ?? 8));
        }

        return result;
    }
}

public sealed record WorkoutSetViewModel(
    SetEntry Set,
    WorkoutExercise Exercise,
    bool Locked,
    WorkoutBuilderSet? Prescription,
    bool HasTarget);
