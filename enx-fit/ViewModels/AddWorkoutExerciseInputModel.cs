using System.ComponentModel.DataAnnotations;

namespace enx_fit.ViewModels;

public class AddWorkoutExerciseInputModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Выберите упражнение.")]
    [Display(Name = "Упражнение")]
    public int ExerciseId { get; set; }
}
