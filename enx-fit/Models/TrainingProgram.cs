using System.ComponentModel.DataAnnotations;

namespace enx_fit.Models;

public enum ProgramLevel { Beginner, Intermediate, Advanced }
public enum ProgressionMethod { Manual, Fixed, DoubleProgression, Rir, Rpe, PercentOneRepMax }

public sealed class TrainingProgram
{
    public int Id { get; set; }
    public string? OwnerId { get; set; }
    [MaxLength(160)] public string Name { get; set; } = "";
    [MaxLength(120)] public string Goal { get; set; } = "";
    [MaxLength(2000)] public string? Description { get; set; }
    public ProgramLevel Level { get; set; }
    public int Weeks { get; set; } = 8;
    public int DaysPerWeek { get; set; } = 3;
    public bool IsTemplate { get; set; }
    public bool RequiresPro { get; set; }
    [MaxLength(250)] public string Categories { get; set; } = "";
    public bool IsArchived { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid Revision { get; set; } = Guid.NewGuid();
    public List<ProgramBlock> Blocks { get; set; } = [];
    public List<ProgramWorkout> Workouts { get; set; } = [];
    public AssignedProgram? Assignment { get; set; }
}

public sealed class ProgramBlock
{
    public int Id { get; set; }
    public int TrainingProgramId { get; set; }
    public int StartWeek { get; set; }
    public int EndWeek { get; set; }
    [MaxLength(120)] public string Name { get; set; } = "";
}

// A prescription is planned work. Actual sets remain in WorkoutExercise / SetEntry.
public sealed class ProgramWorkout
{
    public int Id { get; set; }
    public int TrainingProgramId { get; set; }
    public Guid Key { get; set; } = Guid.NewGuid();
    [MaxLength(120)] public string Name { get; set; } = "";
    public int Order { get; set; }
    public int? DayOfWeek { get; set; } // ISO: Monday = 1 … Sunday = 7. null = unscheduled.
    public int EstimatedMinutes { get; set; } = 60;
    public List<ProgramWorkoutExercise> Exercises { get; set; } = [];
}

public sealed class ProgramWorkoutExercise
{
    public int Id { get; set; }
    public int ProgramWorkoutId { get; set; }
    public int ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;
    public int Order { get; set; }
    public ExercisePrescription Prescription { get; set; } = new();
    public ProgressionRule Progression { get; set; } = new();
}

public sealed class ExercisePrescription
{
    [Range(1, 20)] public int Sets { get; set; } = 3;
    [Range(1, 100)] public int RepsMin { get; set; } = 8;
    [Range(1, 100)] public int RepsMax { get; set; } = 12;
    [Range(0, 1500)] public decimal? WeightKg { get; set; }
    [Range(1, 100)] public decimal? PercentOneRepMax { get; set; }
    [Range(0, 10)] public int? Rir { get; set; }
    [Range(1, 10)] public decimal? Rpe { get; set; }
    [Range(0, 1800)] public int? RestSeconds { get; set; }
    [MaxLength(500)] public string? Comment { get; set; }
    public string Summary => $"{Sets} × {RepsMin}–{RepsMax}";
}

public sealed class ProgressionRule
{
    [EnumDataType(typeof(ProgressionMethod))] public ProgressionMethod Method { get; set; }
    [Range(0.1, 100)] public decimal StepKg { get; set; } = 2.5m;
    [Range(0.1, 100)] public decimal IncreasePercent { get; set; } = 5;
    [Range(1, 100)] public int MinReps { get; set; } = 8;
    [Range(1, 100)] public int MaxReps { get; set; } = 12;
    [Range(0, 10)] public int TargetRir { get; set; } = 2;
    [Range(1, 10)] public decimal TargetRpe { get; set; } = 8;
    [Range(1, 1500)] public decimal OneRepMaxKg { get; set; } = 100;
    [Range(1, 100)] public decimal PercentOneRepMax { get; set; } = 75;
}

public sealed class AssignedProgram
{
    public int Id { get; set; }
    public int ProgramId { get; set; }
    public TrainingProgram Program { get; set; } = null!;
    public int? SourceProgramId { get; set; }
    public string TrainerId { get; set; } = "";
    public string ClientId { get; set; } = "";
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
}
