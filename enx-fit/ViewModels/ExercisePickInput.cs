using System.ComponentModel.DataAnnotations;

namespace enx_fit.ViewModels;

public sealed class ExercisePickInput
{
    public int ExerciseId { get; set; }
    [Range(1, 20)] public int SetsCount { get; set; } = 3;
    [Range(1, 1000)] public int Reps { get; set; } = 12;
    [Range(typeof(decimal), "0", "2000")] public decimal Weight { get; set; }
    [Range(0, 900)] public int RestSeconds { get; set; } = 90;
}
