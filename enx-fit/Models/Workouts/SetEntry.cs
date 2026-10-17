using System.ComponentModel.DataAnnotations.Schema;

namespace enx_fit.Models;

public class SetEntry
{
    public int Id { get; set; }

    public int WorkoutExerciseId { get; set; }

    public WorkoutExercise WorkoutExercise { get; set; } = null!;

    public int SetNumber { get; set; }

    public int Reps { get; set; }

    public decimal Weight { get; set; }

    public bool IsBodyweight { get; set; }

    public List<SetDrop> Drops { get; set; } = [];

    [NotMapped]
    public decimal Volume => (IsBodyweight ? 0 : Weight * Reps) + Drops.Sum(d => d.IsBodyweight ? 0 : d.Weight * d.Reps);

    public int? Rir { get; set; }

    public bool IsWarmup { get; set; }
    public bool IsCompleted { get; set; } = true;
    public bool IsSkipped { get; set; }
    public int? RestSeconds { get; set; }
    public DateTime? PerformedAtUtc { get; set; }

    public string? Notes { get; set; }
}

// Additional stages belong to one set and share its completion, warm-up and rest.
public class SetDrop
{
    public int Id { get; set; }
    public int Order { get; set; }
    public decimal Weight { get; set; }
    public int Reps { get; set; }
    public bool IsBodyweight { get; set; }
}
