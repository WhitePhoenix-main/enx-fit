using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using enx_fit.Models;
using enx_fit.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace enx_fit.Services;

public sealed record WorkoutTemplateOption(int Id, string Name, string ProgramName, int Exercises);

public partial class WorkoutService
{
    public Task<WorkoutSession?> ActiveAsync() => dbContext.WorkoutSessions.AsNoTracking()
        .Include(w => w.WorkoutExercises).Where(w => w.UserId == currentUser.Id).Where(WorkoutStates.Active)
        .OrderByDescending(w => w.CreatedAtUtc).FirstOrDefaultAsync();

    // ProgramWorkout already represents a reusable prescription. Do not introduce a second template store.
    public async Task<List<WorkoutTemplateOption>> TemplatesAsync()
    {
        return await (from w in dbContext.Set<ProgramWorkout>().AsNoTracking()
                      join p in dbContext.TrainingPrograms on w.TrainingProgramId equals p.Id
                      where !p.IsArchived && (p.OwnerId == currentUser.Id || p.IsTemplate) && w.Exercises.Any()
                      orderby p.IsTemplate, p.Name, w.Order
                      select new WorkoutTemplateOption(w.Id, w.Name, p.Name, w.Exercises.Count)).ToListAsync();
    }

    public async Task<int> StartAsync(string source, int? templateId = null, int? plannedId = null, int? previousId = null)
    {
        if (await ActiveAsync() is { } active) return active.Id;
        var today = currentUser.LocalToday;
        WorkoutSession session;
        if (source == "planned")
        {
            session = await dbContext.WorkoutSessions.Include(w => w.WorkoutExercises).ThenInclude(e => e.SetEntries)
                .SingleOrDefaultAsync(w => w.Id == plannedId && w.UserId == currentUser.Id && w.CompletedAtUtc == null)
                ?? throw new InvalidOperationException("Запланированная тренировка недоступна.");
            MaterializeBuilder(session);
            if (session.IsFinished) throw new InvalidOperationException("Эта тренировка уже завершена.");
        }
        else
        {
            session = new() { UserId = currentUser.Id, Title = "Свободная тренировка" };
            if (source == "template") await CopyTemplateAsync(session, templateId ?? 0);
            else if (source == "previous")
            {
                var previous = await dbContext.WorkoutSessions.AsNoTracking().Include(w => w.WorkoutExercises).ThenInclude(e => e.SetEntries)
                    .Where(w => w.UserId == currentUser.Id && w.Date <= today && (!previousId.HasValue || w.Id == previousId.Value)).Where(WorkoutStates.Completed)
                    .OrderByDescending(w => w.Date).ThenByDescending(w => w.Id).FirstOrDefaultAsync()
                    ?? throw new InvalidOperationException(previousId.HasValue ? "Эта тренировка недоступна для повторения." : "Сначала завершите первую тренировку.");
                session.Title = previous.Title;
                session.WorkoutExercises = previous.WorkoutExercises.OrderBy(e => e.Order).Select(e => new WorkoutExercise
                {
                    ExerciseId = e.ExerciseId, Order = e.Order, Notes = e.Notes,
                    TargetSets = e.TargetSets, TargetRepsMin = e.TargetRepsMin, TargetRepsMax = e.TargetRepsMax,
                    TargetWeightKg = e.TargetWeightKg, TargetRir = e.TargetRir, TargetRpe = e.TargetRpe,
                    TargetRestSeconds = e.TargetRestSeconds, BlockKind = e.BlockKind,
                    SetEntries = e.SetEntries.Where(s => s.IsCompleted).OrderBy(s => s.SetNumber).Select(s => new SetEntry
                    { SetNumber = s.SetNumber, Weight = s.Weight, Reps = s.Reps, RestSeconds = s.RestSeconds, IsWarmup = s.IsWarmup,
                        Notes = s.Notes, IsCompleted = false }).ToList()
                }).ToList();
            }
            else if (source != "empty") throw new InvalidOperationException("Выберите способ начала тренировки.");
            dbContext.WorkoutSessions.Add(session);
        }
        session.StartedAtUtc = DateTime.UtcNow;
        session.Status = WorkoutStatus.InProgress;
        session.UtcOffsetMinutes = currentUser.UtcOffsetMinutes;
        session.Revision = Guid.NewGuid();
        session.Date = today;
        try { await dbContext.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            // The unique filtered index arbitrates simultaneous starts in two tabs.
            dbContext.ChangeTracker.Clear();
            if (await ActiveAsync() is { } existing) return existing.Id;
            throw;
        }
        return session.Id;
    }

    public async Task LoadTemplateAsync(int id, int templateId)
    {
        var session = await EditableSessionAsync(id);
        if (session.WorkoutExercises.Count != 0)
            throw new InvalidOperationException("Загрузить шаблон можно в пустую тренировку. Уже записанные упражнения сохранены.");
        await CopyTemplateAsync(session, templateId);
        await SaveCommandAsync(session, null);
    }

    private async Task CopyTemplateAsync(WorkoutSession session, int id)
    {
        var template = await dbContext.Set<ProgramWorkout>().AsNoTracking().Include(w => w.Exercises)
            .SingleOrDefaultAsync(w => w.Id == id && dbContext.TrainingPrograms.Any(p => p.Id == w.TrainingProgramId &&
                !p.IsArchived && (p.OwnerId == currentUser.Id || p.IsTemplate)))
            ?? throw new InvalidOperationException("Шаблон недоступен.");
        if (template.Exercises.Count == 0) throw new InvalidOperationException("В шаблоне пока нет упражнений.");
        var program = await dbContext.TrainingPrograms.AsNoTracking().SingleAsync(p => p.Id == template.TrainingProgramId);
        session.Title = template.Name;
        session.SourceProgramWorkoutId = template.Id;
        session.SourceProgramRevision = program.Revision;
        session.BuilderConfigurationJson = null;
        session.WorkoutExercises = CopyPrescription(template);
        session.SourceStructureJson = Structure(session);
    }

    internal static List<WorkoutExercise> CopyPrescription(ProgramWorkout template) => template.Exercises.OrderBy(e => e.Order)
        .Select(e => new WorkoutExercise
        {
            ExerciseId = e.ExerciseId, Order = e.Order,
            Notes = string.Join(" · ", new[] { e.Prescription.Comment,
                e.Prescription.PercentOneRepMax is { } percent ? $"{percent}% 1ПМ" : null,
                e.Prescription.RestSeconds is { } rest ? $"Отдых {rest} с" : null }.Where(x => !string.IsNullOrWhiteSpace(x))),
            TargetSets = e.Prescription.Sets, TargetRepsMin = e.Prescription.RepsMin, TargetRepsMax = e.Prescription.RepsMax,
            TargetWeightKg = e.Prescription.WeightKg, TargetRir = e.Prescription.Rir, TargetRpe = e.Prescription.Rpe,
            TargetRestSeconds = e.Prescription.RestSeconds,
            SetEntries = Enumerable.Range(1, e.Prescription.Sets).Select(n => new SetEntry
            { SetNumber = n, Weight = e.Prescription.WeightKg ?? 0, Reps = e.Prescription.RepsMin,
                RestSeconds = e.Prescription.RestSeconds, IsCompleted = false }).ToList()
        }).ToList();

    private static void MaterializeBuilder(WorkoutSession session)
    {
        if (session.BuilderConfigurationJson is null || !WorkoutBuilderInput.TryParse(session.BuilderConfigurationJson, out var builder)) return;
        foreach (var exercise in session.WorkoutExercises.Where(e => e.SetEntries.Count == 0))
        {
            var block = builder!.Blocks.FirstOrDefault(b => b.Exercises.Any(e => e.ExerciseId == exercise.ExerciseId));
            var source = block?.Exercises.FirstOrDefault(e => e.ExerciseId == exercise.ExerciseId);
            if (source is null) continue;
            exercise.BlockKind = block!.Kind;
            exercise.SetEntries = source.Sets.Select((s, i) => new SetEntry
            { SetNumber = i + 1, Weight = s.Weight, Reps = s.Reps, RestSeconds = s.RestSeconds,
                IsWarmup = block.Kind == "warmup", IsCompleted = false }).ToList();
        }
    }

    internal static string Structure(WorkoutSession session) => JsonSerializer.Serialize(session.WorkoutExercises
        .OrderBy(e => e.Order).ThenBy(e => e.Id).Select(e => new { e.ExerciseId, Sets = e.SetEntries.Count }));

    private async Task<WorkoutSession> EditableSessionAsync(int id)
    {
        var session = await VisibleWorkouts().Include(w => w.WorkoutExercises).ThenInclude(e => e.SetEntries)
            .SingleOrDefaultAsync(w => w.Id == id) ?? throw new KeyNotFoundException();
        if (session.IsFinished && (session.Status != WorkoutStatus.Legacy || session.StartedAtUtc.HasValue))
            throw new InvalidOperationException("Тренировка уже завершена. Результаты сохранены.");
        return session;
    }

    public async Task ChangeExerciseAsync(int id, int exerciseEntryId, string action, int replacementId = 0)
    {
        var session = await EditableSessionAsync(id);
        var ordered = session.WorkoutExercises.OrderBy(e => e.Order).ThenBy(e => e.Id).ToList();
        var exercise = ordered.SingleOrDefault(e => e.Id == exerciseEntryId) ?? throw new KeyNotFoundException();
        if (action == "remove") { dbContext.WorkoutExercises.Remove(exercise); ordered.Remove(exercise); }
        else if (action == "replace")
        {
            if (exercise.SetEntries.Any(s => s.IsCompleted))
                throw new InvalidOperationException("В упражнении есть выполненные подходы. Добавьте другое упражнение, чтобы сохранить результаты.");
            if (!await dbContext.Exercises.AnyAsync(e => e.Id == replacementId)) throw new InvalidOperationException("Выберите упражнение из библиотеки.");
            if (ordered.Any(e => e.ExerciseId == replacementId)) throw new InvalidOperationException("Это упражнение уже в тренировке.");
            exercise.ExerciseId = replacementId;
            // Results belong to the old exercise; a replacement starts with fresh, uncompleted sets.
            foreach (var set in exercise.SetEntries) { set.IsCompleted = false; set.Weight = 0; set.Rir = null; set.Notes = null; }
            exercise.Notes = null;
            exercise.TargetWeightKg = null; exercise.TargetRir = null; exercise.TargetRpe = null;
        }
        else if (action is "up" or "down")
        {
            var index = ordered.IndexOf(exercise);
            var target = Math.Clamp(index + (action == "up" ? -1 : 1), 0, ordered.Count - 1);
            (ordered[index], ordered[target]) = (ordered[target], ordered[index]);
        }
        else throw new InvalidOperationException("Неизвестное действие.");
        for (var i = 0; i < ordered.Count; i++) ordered[i].Order = i;
        await SaveCommandAsync(session, null);
    }

    public async Task<WorkoutExecutionState> ChangeSetAsync(int id, int setId, AddSetEntryInputModel input, bool completed, bool remove,
        Guid? operationId = null, Guid? expectedRevision = null, DateTime? performedAtUtc = null, bool skipped = false)
    {
        if (await CommandExistsAsync(id, operationId)) return await ExecutionStateAsync(id);
        var session = await EditableSessionAsync(id);
        CheckRevision(session, expectedRevision);
        if (session.State == WorkoutStatus.Paused) throw new InvalidOperationException("Продолжите тренировку перед записью подхода.");
        var set = session.WorkoutExercises.SelectMany(e => e.SetEntries).SingleOrDefault(s => s.Id == setId)
            ?? throw new KeyNotFoundException();
        if (remove) dbContext.SetEntries.Remove(set);
        else
        {
            Validator.ValidateObject(input, new ValidationContext(input), true);
            if (completed && input.Reps < 1) throw new InvalidOperationException("У выполненного подхода должно быть хотя бы одно повторение.");
            var justCompleted = completed && !set.IsCompleted;
            set.Weight = input.Weight; set.Reps = input.Reps; set.Rir = input.Rir;
            set.IsWarmup = input.IsWarmup; set.Notes = input.Notes?.Trim(); set.IsCompleted = completed;
            set.IsSkipped = !completed && skipped;
            set.RestSeconds = input.RestSeconds;
            if (justCompleted)
            {
                var now = DateTime.UtcNow;
                var at = performedAtUtc is { } candidate && candidate <= now && candidate >= session.StartedAtUtc ? candidate : now;
                set.PerformedAtUtc = at;
                if (session.IsActive)
                {
                    var rest = set.RestSeconds ?? session.WorkoutExercises.Single(e => e.Id == set.WorkoutExerciseId).TargetRestSeconds ?? 90;
                    session.RestEndsAtUtc = rest > 0 ? at.AddSeconds(rest) : null;
                    session.RestAfterSetId = rest > 0 ? set.Id : null;
                    session.RestRemainingSeconds = null;
                }
            }
            if (!completed) set.PerformedAtUtc = null;
        }
        if ((remove || !completed) && session.RestAfterSetId == set.Id)
        { session.RestEndsAtUtc = null; session.RestRemainingSeconds = null; session.RestAfterSetId = null; }
        await SaveCommandAsync(session, operationId);
        return await ExecutionStateAsync(id);
    }

    public async Task<bool> CanUpdateTemplateAsync(WorkoutSession session) => session.SourceProgramWorkoutId is { } source &&
        await dbContext.Set<ProgramWorkout>().AnyAsync(w => w.Id == source && dbContext.TrainingPrograms.Any(p =>
            p.Id == w.TrainingProgramId && p.OwnerId == currentUser.Id && !p.IsTemplate && !p.IsArchived && p.Revision == session.SourceProgramRevision));

    public async Task DecideTemplateAsync(int id, string choice, string? name, Guid? operationId = null)
    {
        if (await CommandExistsAsync(id, operationId)) return;
        var session = await VisibleWorkouts().Include(w => w.WorkoutExercises).ThenInclude(e => e.SetEntries)
            .SingleOrDefaultAsync(w => w.Id == id) ?? throw new KeyNotFoundException();
        if (!session.CompletedAtUtc.HasValue) throw new InvalidOperationException("Сначала завершите тренировку.");
        if (choice is not ("keep" or "update" or "new")) throw new InvalidOperationException("Выберите действие с шаблоном.");
        if (!session.TemplateDecisionPending && choice != "new") return;
        if (choice != "keep")
        {
            if (session.UserId != currentUser.Id) throw new KeyNotFoundException();
            var entries = session.WorkoutExercises.OrderBy(e => e.Order).Select(e => new
            {
                Exercise = e,
                Sets = e.SetEntries.Where(s => choice != "new" || s.IsCompleted && !s.IsSkipped).OrderBy(s => s.SetNumber).ToList()
            }).Where(e => e.Sets.Count > 0).ToList();
            if (entries.Count == 0 || entries.Any(e => e.Sets.Count > 20 || e.Sets.Any(s => s.Reps is < 1 or > 100 || s.Weight > 1500)))
                throw new InvalidOperationException("Для шаблона укажите 1–20 подходов, 1–100 повторений и вес до 1500 кг в каждом упражнении.");
            ProgramWorkout template;
            TrainingProgram program;
            if (choice == "update")
            {
                if (!await CanUpdateTemplateAsync(session)) throw new InvalidOperationException("Шаблон изменился или недоступен для редактирования. Сохраните новый шаблон.");
                template = await dbContext.Set<ProgramWorkout>().Include(w => w.Exercises).SingleAsync(w => w.Id == session.SourceProgramWorkoutId);
                program = await dbContext.TrainingPrograms.SingleAsync(p => p.Id == template.TrainingProgramId);
                if (program.Revision != session.SourceProgramRevision)
                    throw new InvalidOperationException("Шаблон изменился. Сохраните новый шаблон.");
                // Keep matching rows and their progression settings/recommendation history.
                var removed = template.Exercises.Where(e => session.WorkoutExercises.All(x => x.ExerciseId != e.ExerciseId)).ToList();
                dbContext.RemoveRange(removed);
                foreach (var exercise in removed) template.Exercises.Remove(exercise);
            }
            else if (choice == "new")
            {
                if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 120) throw new InvalidOperationException("Название шаблона: от 1 до 120 символов.");
                template = new() { Name = name.Trim() };
                program = new() { OwnerId = currentUser.Id, Name = name.Trim(), Goal = "Моя тренировка", Weeks = 1, DaysPerWeek = 1, Workouts = [template] };
                dbContext.TrainingPrograms.Add(program);
            }
            else throw new InvalidOperationException("Выберите действие с шаблоном.");
            foreach (var snapshot in entries)
            {
                var exercise = snapshot.Exercise;
                var entry = template.Exercises.SingleOrDefault(e => e.ExerciseId == exercise.ExerciseId);
                if (entry is null) { entry = new() { ExerciseId = exercise.ExerciseId }; template.Exercises.Add(entry); }
                entry.Order = exercise.Order;
                entry.Prescription = new()
                {
                    Sets = snapshot.Sets.Count, RepsMin = snapshot.Sets.Min(s => s.Reps),
                        RepsMax = snapshot.Sets.Max(s => s.Reps), WeightKg = snapshot.Sets.Max(s => s.Weight),
                        Rir = snapshot.Sets.First().Rir, Rpe = exercise.TargetRpe,
                        RestSeconds = entry.Prescription.RestSeconds, Comment = entry.Prescription.Comment
                };
            }
            program.Revision = Guid.NewGuid();
        }
        session.TemplateDecisionPending = false;
        await SaveCommandAsync(session, operationId);
    }
}
