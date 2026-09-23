using enx_fit.Models;

namespace enx_fit.Services;

public sealed record TrainingInsight(RecommendationType Type, string Reason, string SuggestedChange, decimal CurrentWeight, decimal SuggestedWeight);
public sealed record ProgressionAnalysis(TrainingInsight? Insight, string? UnavailableReason);

public interface IProgressionService
{
    ProgressionAnalysis Analyze(ProgramWorkoutExercise exercise, IReadOnlyList<ExercisePerformance> history);
}

// Pure deterministic calculations. Access and persistence belong to RecommendationService.
public sealed class ProgressionService : IProgressionService
{
    public ProgressionAnalysis Analyze(ProgramWorkoutExercise exercise, IReadOnlyList<ExercisePerformance> history)
    {
        var target = exercise.Prescription;
        if (history.Count == 0) return new(null, "Пока недостаточно данных. Завершите одну тренировку с этим упражнением в программе.");
        if (target.WeightKg is not > 0 || target.PercentOneRepMax.HasValue)
            return new(null, "Укажите рабочий вес в килограммах. Расчёт для собственного веса и % 1ПМ пока не поддерживается.");
        var last = history[0];
        if (last.Date < DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-21))
            return new(null, "Последняя тренировка была более 21 дня назад. Запишите новую тренировку для актуальной рекомендации.");
        if (!ProgramTrainingDataService.Comparable(last, target))
            return new(null, "Для расчёта нужна завершённая тренировка по текущему плану: все рабочие подходы с заданным весом и диапазоном. Разминочные подходы не учитываются.");
        var weight = target.WeightKg.Value;
        var targetRir = exercise.Progression.Method == ProgressionMethod.Rir ? exercise.Progression.TargetRir : target.Rir;
        // RPE is a prescription only: the existing diary records RIR, not actual RPE.
        if (target.Rpe.HasValue)
            return new(new(RecommendationType.KeepWeight, "В плане задан RPE, но фактический RPE в дневнике не записывается. Данных для повышения нагрузки недостаточно.",
                "Оставить текущий вес; оценить нагрузку самостоятельно или использовать RIR.", weight, weight), null);
        var repsReached = last.Sets.All(s => s.Reps >= target.RepsMax);
        var reserveReached = !targetRir.HasValue || last.Sets.All(s => s.Rir.HasValue && s.Rir >= targetRir.Value);
        if (repsReached && reserveReached)
        {
            var nextWeight = Math.Round(weight + exercise.Progression.StepKg, 2);
            if (nextWeight > 1500) return new(null, "Рекомендуемый вес выходит за поддерживаемый диапазон. Измените нагрузку вручную.");
            return new(new(RecommendationType.IncreaseWeight,
                $"Верхняя граница {target.RepsMax} повторений выполнена во всех {target.Sets} рабочих подходах" +
                (targetRir.HasValue ? $" с RIR не ниже {targetRir}." : ". Целевой RIR не задан.") + $" Настроенный шаг — {exercise.Progression.StepKg:0.##} кг.",
                $"Повысить вес до {nextWeight:0.##} кг и начать с нижней границы {target.RepsMin}–{target.RepsMax} повторений.", weight, nextWeight), null);
        }
        return new(new(RecommendationType.KeepWeight,
            !repsReached ? "Верхняя граница диапазона ещё не достигнута во всех рабочих подходах." :
                last.Sets.Any(s => !s.Rir.HasValue) ? "Повторения выполнены, но RIR записан не во всех подходах. Запаса повторений для повышения веса подтвердить нельзя." :
                "Повторения выполнены, но фактический RIR ниже целевого хотя бы в одном подходе.",
            $"Оставить {weight:0.##} кг. Цель: {target.Sets} × {target.RepsMax}" + (targetRir.HasValue ? $" с RIR не ниже {targetRir}." : "."), weight, weight), null);
    }
}
