namespace enx_fit.ViewModels;

public sealed record PreviousWorkingSet(int Number, decimal Weight, int Reps, int? Rir);
public sealed record PreviousExerciseResult(int WorkoutId, DateOnly Date, string Title, IReadOnlyList<PreviousWorkingSet> Sets);
