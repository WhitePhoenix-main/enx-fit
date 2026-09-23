using System.ComponentModel.DataAnnotations;

namespace enx_fit.Models;

public enum RecommendationType { KeepWeight, IncreaseWeight, PossiblePlateau, Deload }
public enum RecommendationStatus { New, Accepted, Dismissed, Expired }

public sealed class TrainingRecommendation
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public int TrainingProgramId { get; set; }
    public int ProgramWorkoutExerciseId { get; set; }
    public int ExerciseId { get; set; }
    public RecommendationType Type { get; set; }
    public RecommendationStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAtUtc { get; set; }
    public Guid ProgramRevision { get; set; }
    [MaxLength(64)] public string EvidenceKey { get; set; } = "";
    [MaxLength(1200)] public string Reason { get; set; } = "";
    [MaxLength(600)] public string SuggestedChange { get; set; } = "";
    public decimal CurrentValue { get; set; }
    public decimal SuggestedValue { get; set; }
}
