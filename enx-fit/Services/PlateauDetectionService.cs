using enx_fit.Models;

namespace enx_fit.Services;

public interface IPlateauDetectionService
{
    TrainingInsight? Analyze(ProgramWorkoutExercise exercise, IReadOnlyList<ExercisePerformance> history);
}

public sealed class PlateauDetectionService : IPlateauDetectionService
{
    public TrainingInsight? Analyze(ProgramWorkoutExercise exercise, IReadOnlyList<ExercisePerformance> history)
    {
        var recent = history.Take(4).ToList();
        var target = exercise.Prescription;
        if (recent.Count < 4 || target.WeightKg is not > 0 || target.PercentOneRepMax.HasValue ||
            recent.Any(p => !ProgramTrainingDataService.Comparable(p, target)) ||
            recent[0].Date < DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-21)) return null;
        var days = recent[0].Date.DayNumber - recent[^1].Date.DayNumber;
        if (days < 14 || recent.Max(p => p.EstimatedMax) > recent.Min(p => p.EstimatedMax) * 1.01m ||
            recent[0].TotalReps > recent[^1].TotalReps || recent[0].Sets.All(s => s.Reps >= target.RepsMax)) return null;
        var weight = target.WeightKg.Value;
        var suggested = Math.Round(weight * .95m * 2, MidpointRounding.AwayFromZero) / 2;
        if (suggested <= 0 || suggested >= weight) return null;
        return new(RecommendationType.PossiblePlateau,
            $"За последние 4 сопоставимые тренировки ({days} дней) оценка 1ПМ по формуле вес × (1 + повторения / 30) менялась не более чем на 1%. Сумма повторений не выросла, верхняя граница диапазона не достигнута. Это возможное плато, а не диагноз.",
            $"Временно снизить вес с {weight:0.##} до {suggested:0.##} кг (около 5%, округление до 0,5 кг), сохранив диапазон {target.RepsMin}–{target.RepsMax}. Можно отклонить рекомендацию и продолжить с текущим весом.", weight, suggested);
    }
}
