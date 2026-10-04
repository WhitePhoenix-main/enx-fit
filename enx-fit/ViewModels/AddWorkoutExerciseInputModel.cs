using System.ComponentModel.DataAnnotations;

namespace enx_fit.ViewModels;

public class AddWorkoutExerciseInputModel
{
    [DeniedValues(0, ErrorMessage = "Выберите упражнение.")]
    [Display(Name = "Упражнение")]
    public int ExerciseId { get; set; }
}
