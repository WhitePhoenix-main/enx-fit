using System.ComponentModel.DataAnnotations;

namespace enx_fit.ViewModels;

public class AddWorkoutExerciseInputModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Select an exercise.")]
    [Display(Name = "Exercise")]
    public int ExerciseId { get; set; }
}
