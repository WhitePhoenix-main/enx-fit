using System.ComponentModel.DataAnnotations;
using enx_fit.Models;
using enx_fit.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace enx_fit.Services;

public partial class WorkoutService
{
    public async Task PickExercisesAsync(int id, IReadOnlyList<ExercisePickInput> picks, int replaceEntryId,
        Guid? operationId, Guid? expectedRevision)
    {
        if (await CommandExistsAsync(id, operationId)) return;
        var session = await EditableSessionAsync(id);
        CheckRevision(session, expectedRevision);
        if (picks.Count is < 1 or > 40 || picks.Select(p => p.ExerciseId).Distinct().Count() != picks.Count)
            throw new InvalidOperationException("Выберите от 1 до 40 разных упражнений.");
        foreach (var pick in picks) Validator.ValidateObject(pick, new ValidationContext(pick), true);
        var ids = picks.Select(p => p.ExerciseId).ToArray();
        if (await dbContext.Exercises.CountAsync(e => ids.Contains(e.Id)) != ids.Length)
            throw new InvalidOperationException("Одно из упражнений недоступно. Обновите библиотеку.");
        if (session.WorkoutExercises.Any(e => ids.Contains(e.ExerciseId)))
            throw new InvalidOperationException("Выбранное упражнение уже в тренировке.");
        if (replaceEntryId == 0 && session.WorkoutExercises.Count + picks.Count > 40)
            throw new InvalidOperationException("В тренировке может быть до 40 упражнений.");
        WorkoutExercise? replacement = null;
        if (replaceEntryId != 0)
        {
            if (picks.Count != 1) throw new InvalidOperationException("Для замены выберите одно упражнение.");
            replacement = session.WorkoutExercises.SingleOrDefault(e => e.Id == replaceEntryId) ?? throw new KeyNotFoundException();
            if (replacement.SetEntries.Any(s => s.IsCompleted))
                throw new InvalidOperationException("Есть выполненные подходы. Добавьте другое упражнение, чтобы сохранить результаты.");
        }
        // Validate the entire selection before changing anything; SaveChanges persists it atomically.
        var order = session.WorkoutExercises.Select(e => e.Order).DefaultIfEmpty(0).Max();
        foreach (var pick in picks)
        {
            var entry = replacement ?? new WorkoutExercise { WorkoutSessionId = id, Order = ++order, BlockKind = "strength" };
            if (replacement != null)
            {
                if (entry.SetEntries.Any(s => s.Id == session.RestAfterSetId))
                { session.RestAfterSetId = null; session.RestEndsAtUtc = null; session.RestRemainingSeconds = null; }
                dbContext.SetEntries.RemoveRange(entry.SetEntries); entry.SetEntries.Clear();
                entry.Notes = null; entry.TargetRir = null; entry.TargetRpe = null;
            }
            entry.ExerciseId = pick.ExerciseId;
            entry.TargetSets = pick.SetsCount; entry.TargetRepsMin = entry.TargetRepsMax = pick.Reps;
            entry.TargetWeightKg = pick.Weight; entry.TargetRestSeconds = pick.RestSeconds;
            for (var n = 1; n <= pick.SetsCount; n++)
                entry.SetEntries.Add(new SetEntry { SetNumber = n, Weight = pick.Weight, Reps = pick.Reps, RestSeconds = pick.RestSeconds, IsCompleted = false });
            if (replacement == null) session.WorkoutExercises.Add(entry);
        }
        await SaveCommandAsync(session, operationId);
    }
}
