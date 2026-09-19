using System.ComponentModel.DataAnnotations;
using enx_fit.Models;

namespace enx_fit.ViewModels;

public class ExerciseInputModel
{
    [Required]
    [StringLength(120)]
    [Display(Name = "Название упражнения")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(80)]
    [Display(Name = "Группа мышц")]
    public string MuscleGroup { get; set; } = string.Empty;

    [Required]
    [StringLength(80)]
    [Display(Name = "Оборудование")]
    public string Equipment { get; set; } = string.Empty;

    [StringLength(1000)]
    [Display(Name = "Заметки")]
    public string? Notes { get; set; }

    public static ExerciseInputModel FromExercise(Exercise exercise) => new()
    {
        Name = exercise.Name,
        MuscleGroup = exercise.MuscleGroup,
        Equipment = exercise.Equipment,
        Notes = exercise.Notes
    };
}
