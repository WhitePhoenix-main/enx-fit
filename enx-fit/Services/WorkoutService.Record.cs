using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using enx_fit.Models;
using enx_fit.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace enx_fit.Services;

public partial class WorkoutService
{
    public async Task<int> RecordAsync(RecordWorkoutInput input)
    {
        Validator.ValidateObject(input, new ValidationContext(input), true);
        if (input.ClientRequestId == Guid.Empty) throw new InvalidOperationException("Обновите форму тренировки.");
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddMinutes(input.UtcOffsetMinutes));
        if (input.Date > today || input.Date.Year < 1970) throw new InvalidOperationException("Выберите прошедшую дату или сегодня.");
        List<RecordedExercise>? exercises;
        try { exercises = JsonSerializer.Deserialize<List<RecordedExercise>>(input.ResultsJson, WorkoutBuilderInput.JsonOptions); }
        catch (JsonException) { throw new InvalidOperationException("Проверьте результаты подходов."); }
        bool Valid(object value) => Validator.TryValidateObject(value, new ValidationContext(value), null, true);
        if (exercises is null || exercises.Count is < 1 or > 40 || exercises.Any(e => e is null || !Valid(e) || e.Sets is null || e.Sets.Any(s => s is null || !Valid(s))) ||
            exercises.Select(e => e.ExerciseId).Distinct().Count() != exercises.Count)
            throw new InvalidOperationException("Добавьте 1–40 разных упражнений и 1–20 подходов в каждом. Проверьте вес, повторения и RIR.");
        if (!exercises.Any(e => e.Sets.Any(s => !s.IsWarmup))) throw new InvalidOperationException("Добавьте хотя бы один рабочий подход.");
        var ids = exercises.Select(e => e.ExerciseId).ToArray();
        if (await dbContext.Exercises.CountAsync(e => ids.Contains(e.Id)) != ids.Length) throw new InvalidOperationException("Упражнение недоступно в библиотеке.");
        var payload = JsonSerializer.Serialize(new { input.Date, Title = input.Title.Trim(), input.DurationMinutes,
            Notes = input.Notes?.Trim(), input.UtcOffsetMinutes, Exercises = exercises }, WorkoutBuilderInput.JsonOptions);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
        async Task<int?> ExistingAsync()
        {
            var existing = await dbContext.WorkoutSessions.AsNoTracking().SingleOrDefaultAsync(w => w.UserId == currentUser.Id && w.ClientRequestId == input.ClientRequestId);
            if (existing is null) return null;
            if (existing.ManualPayloadHash != hash) throw new WorkoutConflictException("Эта запись уже сохранена с другими результатами. Откройте её в истории.");
            return existing.Id;
        }
        if (await ExistingAsync() is { } saved) return saved;
        var session = new WorkoutSession
        {
            UserId = currentUser.Id, Title = input.Title.Trim(), Date = input.Date, Notes = input.Notes?.Trim(),
            EntryMode = WorkoutEntryMode.Manual, Status = WorkoutStatus.Completed, CompletedAtUtc = DateTime.UtcNow,
            UtcOffsetMinutes = input.UtcOffsetMinutes, DurationSeconds = input.DurationMinutes * 60,
            ClientRequestId = input.ClientRequestId, ManualPayloadHash = hash,
            WorkoutExercises = exercises.Select((e, i) => new WorkoutExercise
            {
                ExerciseId = e.ExerciseId, Order = i,
                SetEntries = e.Sets.Select((s, n) => new SetEntry
                { SetNumber = n + 1, Weight = s.Weight, Reps = s.Reps, Rir = s.Rir, IsWarmup = s.IsWarmup,
                    Notes = s.Notes?.Trim(), IsCompleted = true }).ToList()
            }).ToList()
        };
        dbContext.Add(session);
        try { await dbContext.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            dbContext.ChangeTracker.Clear();
            if (await ExistingAsync() is { } repeated) return repeated;
            throw;
        }
        return session.Id;
    }

    public async Task<(string Title, List<RecordedExercise> Exercises)> RecordSourceAsync(string source, int? templateId)
    {
        WorkoutSession session = new();
        if (source == "template") await CopyTemplateAsync(session, templateId ?? 0);
        else if (source == "previous")
            session = await dbContext.WorkoutSessions.AsNoTracking().Include(w => w.WorkoutExercises).ThenInclude(e => e.SetEntries)
                .Where(w => w.UserId == currentUser.Id && w.Date <= currentUser.LocalToday).Where(WorkoutStates.Completed)
                .OrderByDescending(w => w.Date).ThenByDescending(w => w.Id).FirstOrDefaultAsync()
                ?? throw new InvalidOperationException("В истории пока нет выполненных тренировок.");
        else throw new InvalidOperationException("Выберите источник тренировки.");
        return (session.Title ?? "Прошедшая тренировка", session.WorkoutExercises.OrderBy(e => e.Order)
            .Select(e => new RecordedExercise { ExerciseId = e.ExerciseId,
                Sets = e.SetEntries.Where(s => source == "template" || (s.IsCompleted && s.Reps > 0)).OrderBy(s => s.SetNumber)
                    .Select(s => new RecordedSet { Weight = s.Weight, Reps = s.Reps, IsWarmup = s.IsWarmup,
                        Rir = source == "previous" ? s.Rir : null, Notes = source == "previous" ? s.Notes : null }).ToList() })
            .Where(e => e.Sets.Count > 0).ToList());
    }
}
