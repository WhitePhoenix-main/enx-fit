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

    public ICollection<SetEntry> SetEntries { get; set; } = [];
}
