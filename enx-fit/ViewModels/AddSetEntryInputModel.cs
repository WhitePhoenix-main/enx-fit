using System.ComponentModel.DataAnnotations;

namespace enx_fit.ViewModels;

public class AddSetEntryInputModel
{
    [Range(1, int.MaxValue)]
    public int WorkoutExerciseId { get; set; }

    [Range(1, 1000)]
    [Display(Name = "Set")]
    public int SetNumber { get; set; } = 1;

    [Range(typeof(decimal), "0", "10000")]
    public decimal Weight { get; set; }

    [Range(0, 1000)]
    public int Reps { get; set; }

    [Range(0, 10)]
    public int? Rir { get; set; }

    [Display(Name = "Warm-up")]
    public bool IsWarmup { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}
