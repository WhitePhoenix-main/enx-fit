namespace enx_fit.Models;

public class WorkoutSession
{
    public int Id { get; set; }
    public int? TrainingProgramId { get; set; }
    public Guid? ProgramWorkoutKey { get; set; }
    public DateOnly? ScheduledDate { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    public string? UserId { get; set; }

    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    public string? Title { get; set; }

    public string? Notes { get; set; }

    public string? BuilderConfigurationJson { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<WorkoutExercise> WorkoutExercises { get; set; } = [];
}
