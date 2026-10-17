using System.Data;
using enx_fit.Data;
using enx_fit.Models;
using enx_fit.Security;
using Microsoft.EntityFrameworkCore;

namespace enx_fit.Services.Programs;

public interface IRecommendationService
{
    Task<List<TrainingRecommendation>> ListAsync(int programId, int exerciseId);
    Task<TrainingRecommendation?> NextForDashboardAsync(TrainingProgram program);
    Task<string?> GenerateAsync(int programId, int exerciseId);
    Task ResolveAsync(int programId, int recommendationId, bool accept, bool confirmed);
}

public sealed class RecommendationService(ApplicationDbContext db, TrainingProgramService programs,
    ProgramTrainingDataService data, IFeatureAccessService features, CurrentUser user,
    IProgressionService progression, IPlateauDetectionService plateau) : IRecommendationService
{
    private async Task<TrainingProgram> AuthorizedAsync(int id, bool tracking = false)
    {
        var p = await programs.FindAsync(id, tracking) ?? throw new ProgramOperationException(ProgramFailure.NotFound);
        var feature = p.OwnerId == user.Id ? Feature.ProgressionRecommendations : Feature.CoachAnalytics;
        if (!await features.CanUseAsync(feature)) throw new ProgramOperationException(ProgramFailure.Forbidden);
        if (p.IsTemplate) throw new ProgramOperationException(ProgramFailure.Forbidden);
        return p;
    }

    public async Task<List<TrainingRecommendation>> ListAsync(int programId, int exerciseId)
    {
        var p = await AuthorizedAsync(programId);
        var exercise = p.Workouts.SelectMany(w => w.Exercises).SingleOrDefault(e => e.Id == exerciseId)
            ?? throw new ProgramOperationException(ProgramFailure.NotFound);
        var key = ProgramTrainingDataService.EvidenceKey(p, exercise, await data.HistoryAsync(p, exerciseId));
        var rows = await db.TrainingRecommendations.AsNoTracking().Where(r => r.TrainingProgramId == programId &&
            r.UserId == p.OwnerId && r.ProgramWorkoutExerciseId == exerciseId).OrderByDescending(r => r.CreatedAtUtc).Take(10).ToListAsync();
        foreach (var row in rows.Where(r => r.Status == RecommendationStatus.New && Stale(r, key, p))) row.Status = RecommendationStatus.Expired;
        return rows;
    }

    public async Task<TrainingRecommendation?> NextForDashboardAsync(TrainingProgram p)
    {
        // This shortcut is exclusively for the viewer's own, already loaded program.
        // External callers cannot use a supplied snapshot to read another owner's data.
        if (p.OwnerId != user.Id || p.IsTemplate || p.IsArchived ||
            !await features.CanUseAsync(Feature.ProgramBasic) || !await features.CanUseAsync(Feature.ProgressionRecommendations))
            throw new ProgramOperationException(ProgramFailure.Forbidden);
        var groups = await db.Set<ProgramWorkout>().AsNoTracking().AsSingleQuery().Where(w => w.TrainingProgramId == p.Id &&
            db.TrainingPrograms.Any(actual => actual.Id == w.TrainingProgramId && actual.OwnerId == user.Id &&
                !actual.IsTemplate && !actual.IsArchived && actual.Revision == p.Revision))
            .SelectMany(w => w.Exercises.Select(e => new
            {
                e.Id,
                Rows = db.TrainingRecommendations.Where(r => r.TrainingProgramId == p.Id && r.UserId == user.Id &&
                    r.ProgramWorkoutExerciseId == e.Id).OrderByDescending(r => r.CreatedAtUtc).Take(10).ToList()
            })).ToListAsync();
        var candidates = groups.SelectMany(g => g.Rows).Where(r => r.Status == RecommendationStatus.New && r.ProgramRevision == p.Revision)
            .OrderByDescending(r => r.CreatedAtUtc).ToList();
        if (candidates.Count == 0) return null;
        var histories = await data.HistoryBatchAsync(p, candidates.Select(r => r.ProgramWorkoutExerciseId).Distinct().ToArray());
        foreach (var group in candidates.GroupBy(r => r.ProgramWorkoutExerciseId))
        {
            var exercise = p.Workouts.SelectMany(w => w.Exercises).Single(e => e.Id == group.Key);
            var key = ProgramTrainingDataService.EvidenceKey(p, exercise, histories[group.Key]);
            if (group.FirstOrDefault(r => !Stale(r, key, p)) is { } proposal) return proposal;
        }
        return null;
    }

    public async Task<string?> GenerateAsync(int programId, int exerciseId)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var p = await AuthorizedAsync(programId);
        if (p.IsArchived) throw new ProgramOperationException(ProgramFailure.Invalid, "Восстановите программу перед анализом.");
        var exercise = p.Workouts.SelectMany(w => w.Exercises).SingleOrDefault(e => e.Id == exerciseId)
            ?? throw new ProgramOperationException(ProgramFailure.NotFound);
        var history = await data.HistoryAsync(p, exerciseId);
        var key = ProgramTrainingDataService.EvidenceKey(p, exercise, history);
        var previous = await db.TrainingRecommendations.Where(r => r.ProgramWorkoutExerciseId == exerciseId).ToListAsync();
        foreach (var row in previous.Where(r => r.Status == RecommendationStatus.New && Stale(r, key, p)))
        { row.Status = RecommendationStatus.Expired; row.ResolvedAtUtc = DateTime.UtcNow; }
        var result = progression.Analyze(exercise, history);
        var insight = await features.CanUseAsync(Feature.AnalyticsPlateauDetection) ? plateau.Analyze(exercise, history) ?? result.Insight : result.Insight;
        if (insight is not null && previous.All(r => r.EvidenceKey != key))
            db.TrainingRecommendations.Add(new()
            {
                UserId = p.OwnerId!, TrainingProgramId = p.Id, ProgramWorkoutExerciseId = exercise.Id, ExerciseId = exercise.ExerciseId,
                ProgramRevision = p.Revision, EvidenceKey = key, Type = insight.Type,
                Reason = insight.Reason, SuggestedChange = insight.SuggestedChange, CurrentValue = insight.CurrentWeight, SuggestedValue = insight.SuggestedWeight
            });
        await db.SaveChangesAsync(); await transaction.CommitAsync();
        var existing = previous.FirstOrDefault(r => r.EvidenceKey == key);
        return result.UnavailableReason ?? (existing?.Status is RecommendationStatus.Dismissed or RecommendationStatus.Expired
            ? "Рекомендация для этих результатов уже отклонена или устарела. После следующей тренировки можно выполнить новый расчёт." : null);
    }

    public async Task ResolveAsync(int programId, int recommendationId, bool accept, bool confirmed)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var p = await AuthorizedAsync(programId, true);
        if (p.IsArchived) throw new ProgramOperationException(ProgramFailure.Invalid, "Программа находится в архиве.");
        var recommendation = await db.TrainingRecommendations.SingleOrDefaultAsync(r => r.Id == recommendationId && r.TrainingProgramId == p.Id && r.UserId == p.OwnerId)
            ?? throw new ProgramOperationException(ProgramFailure.NotFound);
        if (recommendation.Status != RecommendationStatus.New) throw new ProgramOperationException(ProgramFailure.Conflict);
        if (accept && !confirmed) throw new ProgramOperationException(ProgramFailure.Invalid, "Подтвердите применение показанного изменения нагрузки.");
        var exercise = p.Workouts.SelectMany(w => w.Exercises).Single(e => e.Id == recommendation.ProgramWorkoutExerciseId);
        var key = ProgramTrainingDataService.EvidenceKey(p, exercise, await data.HistoryAsync(p, exercise.Id));
        if (Stale(recommendation, key, p))
        {
            recommendation.Status = RecommendationStatus.Expired; recommendation.ResolvedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(); await transaction.CommitAsync();
            throw new ProgramOperationException(ProgramFailure.Invalid, "Данные или программа изменились. Рассчитайте новую рекомендацию.");
        }
        if (accept)
        {
            exercise.Prescription.WeightKg = recommendation.SuggestedValue;
            p.Revision = Guid.NewGuid();
        }
        recommendation.Status = accept ? RecommendationStatus.Accepted : RecommendationStatus.Dismissed;
        recommendation.ResolvedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(); await transaction.CommitAsync();
    }

    private static bool Stale(TrainingRecommendation r, string key, TrainingProgram p) =>
        r.ProgramRevision != p.Revision || r.EvidenceKey != key || r.CreatedAtUtc < DateTime.UtcNow.AddDays(-14) || p.IsArchived;
}
