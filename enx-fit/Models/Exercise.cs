namespace enx_fit.Models;

public class Exercise
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<WorkoutExercise> WorkoutExercises { get; set; } = [];
}
