namespace enx_fit.Models;

public class SetEntry
{
    public int Id { get; set; }

    public int WorkoutExerciseId { get; set; }

    public WorkoutExercise WorkoutExercise { get; set; } = null!;

    public int SetNumber { get; set; }

    public int Reps { get; set; }

    public decimal Weight { get; set; }

    public int? Rir { get; set; }

    public bool IsWarmup { get; set; }

    public string? Notes { get; set; }
}
