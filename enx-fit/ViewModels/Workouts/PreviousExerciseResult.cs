using enx_fit.Models;

namespace enx_fit.ViewModels.Workouts;

public sealed record PreviousWorkingSet(int Number, decimal Weight, int Reps, int? Rir, bool IsBodyweight, List<SetDrop> Drops);
public sealed record PreviousExerciseResult(int WorkoutId, DateOnly Date, string Title, IReadOnlyList<PreviousWorkingSet> Sets);
