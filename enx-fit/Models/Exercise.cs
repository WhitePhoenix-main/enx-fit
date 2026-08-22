namespace enx_fit.Models;

public class Exercise
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string MuscleGroup { get; set; } = string.Empty;

    public string Equipment { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public ICollection<WorkoutExercise> WorkoutExercises { get; set; } = [];
}
