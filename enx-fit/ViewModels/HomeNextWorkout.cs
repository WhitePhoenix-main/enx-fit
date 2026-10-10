namespace enx_fit.ViewModels;

public sealed record HomeNextWorkout(string Title, DateOnly Date, int ExerciseCount,
    int? WorkoutId = null, int? ProgramId = null, int? EstimatedMinutes = null);
