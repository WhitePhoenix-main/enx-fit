using System.ComponentModel.DataAnnotations;
using enx_fit.ModelBinding;

namespace enx_fit.ViewModels.Workouts;

public class AddSetEntryInputModel : IValidatableObject
{
    [Range(1, int.MaxValue)]
    public int WorkoutExerciseId { get; set; }

    [Range(1, 1000)]
    [Display(Name = "Set")]
    public int SetNumber { get; set; } = 1;

    [Range(typeof(decimal), "0", "10000")]
    [Microsoft.AspNetCore.Mvc.ModelBinder(BinderType = typeof(WorkoutWeightBinder))]
    public decimal Weight { get; set; }

    public bool IsBodyweight { get; set; }

    public List<SetDropInput>? Drops { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => SetDropInput.ValidateDrops(Drops);

    [Range(0, 1000)]
    public int Reps { get; set; }

    [Range(0, 10)]
    public int? Rir { get; set; }

    [Display(Name = "Warm-up")]
    public bool IsWarmup { get; set; }

    [Range(0, 1800)] public int? RestSeconds { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}
