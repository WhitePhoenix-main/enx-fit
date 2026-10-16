using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace enx_fit.Models;

public enum WorkoutStatus { Legacy, Planned, InProgress, Paused, Completed, Cancelled }
public enum WorkoutEntryMode { Live, Manual }

public class WorkoutSession
{
    public int Id { get; set; }
    public int? TrainingProgramId { get; set; }
    public Guid? ProgramWorkoutKey { get; set; }
    public DateOnly? ScheduledDate { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public int? SourceProgramWorkoutId { get; set; }
    public Guid? SourceProgramRevision { get; set; }
    public string? SourceStructureJson { get; set; }
    public bool TemplateDecisionPending { get; set; }
    public WorkoutStatus Status { get; set; }
    public WorkoutEntryMode EntryMode { get; set; }
    [ConcurrencyCheck] public Guid Revision { get; set; } = Guid.NewGuid();
    public int UtcOffsetMinutes { get; set; }
    public DateTime? PausedAtUtc { get; set; }
    public int PausedSeconds { get; set; }
    public int? DurationSeconds { get; set; }
    public DateTime? RestEndsAtUtc { get; set; }
    public int? RestRemainingSeconds { get; set; }
    public int? RestAfterSetId { get; set; }
    public Guid? ClientRequestId { get; set; }
    public string? ManualPayloadHash { get; set; }

    // Legacy rows and existing import fixtures retain their original classification.
    [NotMapped] public WorkoutStatus State => Status != WorkoutStatus.Legacy ? Status :
        CompletedAtUtc.HasValue ? WorkoutStatus.Completed :
        StartedAtUtc.HasValue || TrainingProgramId.HasValue ? WorkoutStatus.InProgress :
        WorkoutExercises.Any(e => e.SetEntries.Any(s => s.IsCompleted && !s.IsWarmup && s.Reps > 0))
            ? WorkoutStatus.Completed : WorkoutStatus.Planned;
    [NotMapped] public bool IsActive => State is WorkoutStatus.InProgress or WorkoutStatus.Paused;
    [NotMapped] public bool IsFinished => State is WorkoutStatus.Completed or WorkoutStatus.Cancelled;

    public string? UserId { get; set; }

    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    public string? Title { get; set; }

    public string? Notes { get; set; }

    public string? BuilderConfigurationJson { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<WorkoutExercise> WorkoutExercises { get; set; } = [];
}
