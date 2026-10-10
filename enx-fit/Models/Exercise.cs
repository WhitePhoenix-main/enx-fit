namespace enx_fit.Models;

public class Exercise
{
    [Microsoft.AspNetCore.Mvc.ModelBinder(BinderType = typeof(enx_fit.ViewModels.ExerciseIdModelBinder))]
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string MuscleGroup { get; set; } = string.Empty;

    public string Equipment { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public ICollection<WorkoutExercise> WorkoutExercises { get; set; } = [];
}
