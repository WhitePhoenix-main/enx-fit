namespace enx_fit.Models;

public class WorkoutExercise
{
    public int Id { get; set; }

    public int WorkoutSessionId { get; set; }

    public WorkoutSession WorkoutSession { get; set; } = null!;

    public int ExerciseId { get; set; }

    public Exercise Exercise { get; set; } = null!;

    public int Order { get; set; }

    public string? Notes { get; set; }

    // Immutable prescription at session start; later program edits do not rewrite history.
    public int? TargetSets { get; set; }
    public int? TargetRepsMin { get; set; }
    public int? TargetRepsMax { get; set; }
    public decimal? TargetWeightKg { get; set; }
    public int? TargetRir { get; set; }
    public decimal? TargetRpe { get; set; }

    public ICollection<SetEntry> SetEntries { get; set; } = [];
}
