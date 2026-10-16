namespace enx_fit.Models;

// A receipt saved atomically with the mutation makes network retries idempotent.
public sealed class WorkoutCommand
{
    public int WorkoutSessionId { get; set; }
    public WorkoutSession WorkoutSession { get; set; } = null!;
    public Guid OperationId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
