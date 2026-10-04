using System.ComponentModel.DataAnnotations;

namespace enx_fit.ViewModels;

public sealed class RecordWorkoutInput
{
    [Required, StringLength(160)] public string Title { get; set; } = "Прошедшая тренировка";
    [Required] public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [Range(1, 1440)] public int? DurationMinutes { get; set; }
    [StringLength(1000)] public string? Notes { get; set; }
    [Range(-840, 840)] public int UtcOffsetMinutes { get; set; }
    public Guid ClientRequestId { get; set; } = Guid.NewGuid();
    [Required, StringLength(200000)] public string ResultsJson { get; set; } = "[]";
}

public sealed class RecordedExercise
{
    [Range(1, int.MaxValue)] public int ExerciseId { get; set; }
    [Required, MinLength(1), MaxLength(20)] public List<RecordedSet> Sets { get; set; } = [];
}
public sealed class RecordedSet
{
    [Range(typeof(decimal), "0", "10000")] public decimal Weight { get; set; }
    [Range(1, 1000)] public int Reps { get; set; } = 10;
    [Range(0, 10)] public int? Rir { get; set; }
    public bool IsWarmup { get; set; }
    [StringLength(500)] public string? Notes { get; set; }
}
