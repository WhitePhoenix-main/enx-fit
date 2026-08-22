using System.ComponentModel.DataAnnotations;
using enx_fit.Models;

namespace enx_fit.ViewModels;

public class ExerciseInputModel
{
    [Required]
    [StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(80)]
    [Display(Name = "Muscle group")]
    public string MuscleGroup { get; set; } = string.Empty;

    [Required]
    [StringLength(80)]
    public string Equipment { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Notes { get; set; }

    public static ExerciseInputModel FromExercise(Exercise exercise) => new()
    {
        Name = exercise.Name,
        MuscleGroup = exercise.MuscleGroup,
        Equipment = exercise.Equipment,
        Notes = exercise.Notes
    };
}
