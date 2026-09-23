using enx_fit.Data;
using enx_fit.Security;
using enx_fit.Models;
using enx_fit.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.ComponentModel.DataAnnotations;

namespace enx_fit.Services;

public sealed partial class TrainingProgramService(ApplicationDbContext db, CurrentUser currentUser, IFeatureAccessService features)
{
    public Task<FeatureAccess> AccessAsync() => features.GetAsync();
    private IQueryable<TrainingProgram> Graph() => db.TrainingPrograms.AsSplitQuery()
        .Include(p => p.Assignment).Include(p => p.Blocks)
        .Include(p => p.Workouts).ThenInclude(w => w.Exercises).ThenInclude(e => e.Exercise);

    public async Task<TrainingProgram?> FindAsync(int id, bool tracking = false)
    {
        var access = await features.GetAsync();
        Require(access.CanUse(Feature.ProgramBasic), ProgramFailure.Forbidden);
        var query = Graph().Where(p => p.Id == id && (p.OwnerId == currentUser.Id || p.IsTemplate ||
            currentUser.IsAdministrator || (access.CanUse(Feature.CoachProgramAssignment) && p.Assignment != null &&
                p.Assignment.TrainerId == currentUser.Id && db.Users.Any(u => u.Id == p.OwnerId && u.TrainerId == currentUser.Id))));
        return await (tracking ? query : query.AsNoTracking()).SingleOrDefaultAsync();
    }

    public Task<int> OwnCountAsync() => db.TrainingPrograms.CountAsync(p => p.OwnerId == currentUser.Id && !p.IsTemplate && !p.IsArchived && p.Assignment == null);

    public async Task<List<TrainingProgram>> ListAsync(string tab, bool archived = false, string? category = null)
    {
        var access = await features.GetAsync();
        var query = Graph().AsNoTracking();
        if (tab == "templates") query = query.Where(p => p.IsTemplate && !p.IsArchived);
        else if (tab == "assigned")
        {
            Require(access.CanUse(Feature.CoachProgramAssignment), ProgramFailure.Forbidden);
            query = query.Where(p => p.Assignment != null && (currentUser.IsAdministrator ||
                (p.Assignment.TrainerId == currentUser.Id && db.Users.Any(u => u.Id == p.OwnerId && u.TrainerId == currentUser.Id))) && p.IsArchived == archived);
        }
        else query = query.Where(p => p.OwnerId == currentUser.Id && !p.IsTemplate && p.IsArchived == archived);
        if (tab == "templates" && !string.IsNullOrWhiteSpace(category)) query = query.Where(p => p.Categories.Contains(category));
        return await query.OrderByDescending(p => p.StartDate).ThenByDescending(p => p.CreatedAtUtc).ToListAsync();
    }

    public Task<List<Exercise>> ExercisesAsync() => db.Exercises.AsNoTracking().OrderBy(e => e.Name).ToListAsync();

    private async Task CheckBasicAccessAsync()
    {
        var access = await features.GetAsync();
        Require(access.CanUse(Feature.ProgramBasic), ProgramFailure.Forbidden);
    }

    private async Task<TrainingProgram> EditableAsync(int id)
    {
        await CheckBasicAccessAsync();
        var p = await FindAsync(id, true);
        Require(p is not null, ProgramFailure.NotFound);
        Require(!p!.IsTemplate, ProgramFailure.Forbidden);
        Require(!p.IsArchived, ProgramFailure.Invalid, "Сначала восстановите программу из архива.");
        return p;
    }

    public async Task<int> SaveAsync(int? id, ProgramInput input)
    {
        Validate(input);
        foreach (var workoutInput in input.Workouts)
        {
            Validate(workoutInput);
            foreach (var exerciseInput in workoutInput.Exercises) { Validate(exerciseInput); Validate(exerciseInput.Prescription); }
        }
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        TrainingProgram p;
        if (id.HasValue) p = await EditableAsync(id.Value);
        else { await CheckBasicAccessAsync(); p = new() { OwnerId = currentUser.Id }; db.TrainingPrograms.Add(p); }
        if (id.HasValue) Require(p.Revision == input.Revision, ProgramFailure.Conflict);
        var ids = input.Workouts.SelectMany(w => w.Exercises).Select(e => e.ExerciseId).Distinct().ToArray();
        Require(await db.Exercises.CountAsync(e => ids.Contains(e.Id)) == ids.Length, ProgramFailure.Invalid, "Выберите упражнения из справочника.");
        Require(!p.Blocks.Any(b => b.EndWeek > input.Weeks), ProgramFailure.Invalid, "Сначала скорректируйте блоки периодизации, выходящие за новую длительность.");
        p.Name = input.Name.Trim(); p.Goal = input.Goal.Trim(); p.Description = input.Description?.Trim();
        p.Level = input.Level; p.Weeks = input.Weeks; p.DaysPerWeek = input.DaysPerWeek; p.Revision = Guid.NewGuid();
        // Keep stable workout keys and progression settings when editing prescriptions.
        foreach (var removed in p.Workouts.Where(w => !input.Workouts.Any(i => i.Key == w.Key)).ToList()) db.Remove(removed);
        for (var i = 0; i < input.Workouts.Count; i++)
        {
            var item = input.Workouts[i];
            var workout = p.Workouts.FirstOrDefault(w => item.Key != Guid.Empty && w.Key == item.Key);
            if (workout is null)
            {
                Require(item.Key == Guid.Empty, ProgramFailure.Invalid, "Неизвестная тренировка. Обновите страницу.");
                workout = new(); p.Workouts.Add(workout);
            }
            workout.Name = item.Name.Trim(); workout.Order = i; workout.DayOfWeek = item.DayOfWeek; workout.EstimatedMinutes = item.EstimatedMinutes;
            foreach (var removed in workout.Exercises.Where(e => !item.Exercises.Any(x => x.ExerciseId == e.ExerciseId)).ToList()) db.Remove(removed);
            for (var j = 0; j < item.Exercises.Count; j++)
            {
                var entry = item.Exercises[j];
                var exercise = workout.Exercises.FirstOrDefault(e => e.ExerciseId == entry.ExerciseId);
                if (exercise is null) { exercise = new() { ExerciseId = entry.ExerciseId }; workout.Exercises.Add(exercise); }
                exercise.Order = j; exercise.Prescription = CopyPrescription(entry.Prescription);
            }
        }
        await db.SaveChangesAsync(); await transaction.CommitAsync(); return p.Id;
    }

    public async Task<int> DuplicateAsync(int id)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await CheckBasicAccessAsync();
        var source = await FindAsync(id);
        Require(source is not null, ProgramFailure.NotFound);
        var copy = CopyProgram(source!, currentUser.Id);
        if (!source!.IsTemplate) copy.Name = $"{source.Name[..Math.Min(150, source.Name.Length)]} · копия";
        if (!await features.CanUseAsync(Feature.ProgramAutoProgression))
            foreach (var e in copy.Workouts.SelectMany(w => w.Exercises)) e.Progression = new();
        db.Add(copy); await db.SaveChangesAsync(); await transaction.CommitAsync(); return copy.Id;
    }

    public async Task ArchiveAsync(int id, bool restore)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var p = await FindAsync(id, true);
        Require(p is not null, ProgramFailure.NotFound); Require(!p!.IsTemplate, ProgramFailure.Forbidden);
        await CheckBasicAccessAsync();
        p.IsArchived = !restore; p.Revision = Guid.NewGuid();
        await db.SaveChangesAsync(); await transaction.CommitAsync();
    }

    public async Task<List<ProgramClient>> ClientsAsync()
    {
        Require(await features.CanUseAsync(Feature.CoachProgramAssignment), ProgramFailure.Forbidden);
        return await db.Users.AsNoTracking().Where(u => u.Id != currentUser.Id && (currentUser.IsAdministrator || u.TrainerId == currentUser.Id))
            .OrderBy(u => u.UserName).Select(u => new ProgramClient(u.Id, u.UserName ?? u.Email ?? "Клиент")).ToListAsync();
    }

    public async Task<int> AssignAsync(int id, AssignmentInput input)
    {
        Validate(input);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        Require(await features.CanUseAsync(Feature.CoachProgramAssignment), ProgramFailure.Forbidden);
        Require((await ClientsAsync()).Any(c => c.Id == input.ClientId), ProgramFailure.NotFound);
        var source = await FindAsync(id);
        Require(source is not null, ProgramFailure.NotFound);
        Require(!source!.IsArchived, ProgramFailure.Invalid, "Архивную программу нельзя назначить.");
        Require(input.StartDate >= DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-1) && input.StartDate <= DateOnly.FromDateTime(DateTime.UtcNow).AddYears(2), ProgramFailure.Invalid, "Укажите дату начала в пределах года назад и двух лет вперёд.");
        var copy = CopyProgram(source, input.ClientId);
        var ordered = copy.Workouts.OrderBy(w => w.Order).ToList();
        Require(ordered.Count > 0 && ordered.All(w => w.Exercises.Count > 0), ProgramFailure.Invalid, "Добавьте тренировки и упражнения перед назначением.");
        Require(input.Days.Count == ordered.Count && input.Days.All(d => d is >= 1 and <= 7) && input.Days.Distinct().Count() == input.Days.Count,
            ProgramFailure.Invalid, "Выберите разные дни для каждой тренировки.");
        for (var i = 0; i < ordered.Count; i++) ordered[i].DayOfWeek = input.Days[i];
        copy.DaysPerWeek = ordered.Count; copy.StartDate = input.StartDate;
        copy.Assignment = new() { TrainerId = currentUser.Id, ClientId = input.ClientId, SourceProgramId = source.Id };
        db.Add(copy); await db.SaveChangesAsync(); await transaction.CommitAsync(); return copy.Id;
    }

    public async Task<List<TrainingProgram>> AssignmentsAsync(int sourceId)
    {
        Require(await features.CanUseAsync(Feature.CoachProgramAssignment), ProgramFailure.Forbidden);
        return (await ListAsync("assigned")).Where(p => p.Assignment!.SourceProgramId == sourceId).ToList();
    }

    public async Task SaveProgressionAsync(int id, int exerciseId, ProgressionRule rule)
    {
        Validate(rule);
        var p = await EditableAsync(id);
        Require(rule.Method == ProgressionMethod.Manual || await features.CanUseAsync(Feature.ProgramAutoProgression), ProgramFailure.Upgrade);
        Require(rule.Method is ProgressionMethod.Manual or ProgressionMethod.DoubleProgression or ProgressionMethod.Rir,
            ProgramFailure.Invalid, "Этот метод пока не поддерживается. Выберите ручную прогрессию, диапазон повторений или RIR.");
        Require(rule.MinReps <= rule.MaxReps, ProgramFailure.Invalid, "Проверьте диапазон повторений.");
        var exercise = p.Workouts.SelectMany(w => w.Exercises).SingleOrDefault(e => e.Id == exerciseId);
        Require(exercise is not null, ProgramFailure.NotFound);
        exercise!.Progression = rule; p.Revision = Guid.NewGuid(); await db.SaveChangesAsync();
    }

    public async Task ActivateAsync(int id, DateOnly start)
    {
        var p = await EditableAsync(id);
        Require(p.OwnerId == currentUser.Id, ProgramFailure.Forbidden);
        Require(!p.StartDate.HasValue, ProgramFailure.Invalid, "Программа уже начата.");
        Require(start >= DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-1) && start <= DateOnly.FromDateTime(DateTime.UtcNow).AddYears(2), ProgramFailure.Invalid, "Проверьте дату начала.");
        Require(p.Workouts.Count(w => w.DayOfWeek.HasValue) == p.DaysPerWeek && p.Workouts.All(w => w.Exercises.Count > 0), ProgramFailure.Invalid, "Заполните расписание и добавьте упражнения перед началом.");
        p.StartDate = start; p.Revision = Guid.NewGuid(); await db.SaveChangesAsync();
    }

    public Task<List<WorkoutSession>> SessionsAsync(TrainingProgram p) => db.WorkoutSessions.AsNoTracking().AsSplitQuery()
        .Where(s => s.TrainingProgramId == p.Id && s.UserId == p.OwnerId).Include(s => s.WorkoutExercises).ThenInclude(e => e.SetEntries).ToListAsync();

    public async Task<int> StartNextAsync(int id)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var p = await EditableAsync(id);
        Require(p.OwnerId == currentUser.Id, ProgramFailure.Forbidden);
        var sessions = await SessionsAsync(p);
        var ongoing = sessions.FirstOrDefault(s => s.CompletedAtUtc is null);
        if (ongoing is not null) return ongoing.Id;
        var next = ProgramSchedule.Next(p, sessions);
        Require(next is not null, ProgramFailure.Invalid, "Начните программу и заполните расписание. Если все тренировки выполнены, создайте новый цикл.");
        Require(next!.Workout.Exercises.Count > 0, ProgramFailure.Invalid, "Сначала добавьте упражнения в тренировку.");
        var session = new WorkoutSession
        {
            UserId = currentUser.Id, Title = next.Workout.Name, TrainingProgramId = id,
            ProgramWorkoutKey = next.Workout.Key, ScheduledDate = next.Date,
            WorkoutExercises = next.Workout.Exercises.OrderBy(e => e.Order).Select(e => new WorkoutExercise
            {
                ExerciseId = e.ExerciseId, Order = e.Order,
                TargetSets = e.Prescription.Sets, TargetRepsMin = e.Prescription.RepsMin, TargetRepsMax = e.Prescription.RepsMax,
                TargetWeightKg = e.Prescription.WeightKg, TargetRir = e.Prescription.Rir, TargetRpe = e.Prescription.Rpe,
                Notes = $"План: {e.Prescription.Summary}" + (e.Prescription.WeightKg is { } kg ? $" · {kg} кг" : "") +
                    (e.Prescription.PercentOneRepMax is { } percent ? $" · {percent}% 1ПМ" : "") +
                    (e.Prescription.Rir is { } rir ? $" · RIR {rir}" : "") + (e.Prescription.Rpe is { } rpe ? $" · RPE {rpe}" : "") +
                    (e.Prescription.RestSeconds is { } rest ? $" · отдых {rest} с" : "") + " " + e.Prescription.Comment
            }).ToList()
        };
        db.Add(session); await db.SaveChangesAsync(); await transaction.CommitAsync(); return session.Id;
    }

    private static TrainingProgram CopyProgram(TrainingProgram p, string ownerId) => new()
    {
        OwnerId = ownerId, Name = p.Name, Goal = p.Goal, Description = p.Description, Level = p.Level, Weeks = p.Weeks,
        DaysPerWeek = p.DaysPerWeek,
        Blocks = p.Blocks.Select(b => new ProgramBlock { Name = b.Name, StartWeek = b.StartWeek, EndWeek = b.EndWeek }).ToList(),
        Workouts = p.Workouts.Select(w => new ProgramWorkout
        {
            Name = w.Name, Order = w.Order, DayOfWeek = w.DayOfWeek, EstimatedMinutes = w.EstimatedMinutes,
            Exercises = w.Exercises.Select(e => new ProgramWorkoutExercise
            {
                ExerciseId = e.ExerciseId, Order = e.Order, Prescription = CopyPrescription(e.Prescription),
                Progression = new() { Method = e.Progression.Method, StepKg = e.Progression.StepKg, IncreasePercent = e.Progression.IncreasePercent,
                    MinReps = e.Progression.MinReps, MaxReps = e.Progression.MaxReps, TargetRir = e.Progression.TargetRir,
                    TargetRpe = e.Progression.TargetRpe, OneRepMaxKg = e.Progression.OneRepMaxKg, PercentOneRepMax = e.Progression.PercentOneRepMax }
            }).ToList()
        }).ToList()
    };
    private static ExercisePrescription CopyPrescription(ExercisePrescription p) => new()
    { Sets = p.Sets, RepsMin = p.RepsMin, RepsMax = p.RepsMax, WeightKg = p.WeightKg, PercentOneRepMax = p.PercentOneRepMax, Rir = p.Rir, Rpe = p.Rpe, RestSeconds = p.RestSeconds, Comment = p.Comment };
    private static void Require(bool condition, ProgramFailure failure, string? message = null)
    { if (!condition) throw new ProgramOperationException(failure, message); }

    public async Task SavePrescriptionAsync(int id, int exerciseId, ExercisePrescription input, Guid revision)
    {
        Require(await features.CanUseAsync(Feature.ProgramProgressionManual), ProgramFailure.Forbidden);
        Validate(input);
        Require(input.RepsMin <= input.RepsMax && !(input.WeightKg.HasValue && input.PercentOneRepMax.HasValue), ProgramFailure.Invalid, "Проверьте диапазон повторений и способ задания веса.");
        var p = await EditableAsync(id);
        Require(p.Revision == revision, ProgramFailure.Conflict);
        var exercise = p.Workouts.SelectMany(w => w.Exercises).SingleOrDefault(e => e.Id == exerciseId);
        Require(exercise is not null, ProgramFailure.NotFound);
        exercise!.Prescription = CopyPrescription(input); p.Revision = Guid.NewGuid();
        await db.SaveChangesAsync();
    }

    private static void Validate(object input)
    {
        var errors = new List<ValidationResult>();
        Require(Validator.TryValidateObject(input, new ValidationContext(input), errors, true), ProgramFailure.Invalid,
            string.Join(" ", errors.Select(e => e.ErrorMessage)));
    }
}

public enum ProgramFailure { NotFound, Forbidden, Limit, Upgrade, Invalid, Conflict }
public sealed class ProgramOperationException(ProgramFailure failure, string? message = null) : Exception(message)
{ public ProgramFailure Failure { get; } = failure; }
public sealed record ProgramClient(string Id, string Name);

public sealed record ProgramOccurrence(ProgramWorkout Workout, DateOnly Date);
public static class ProgramSchedule
{
    public static string LevelName(ProgramLevel level) => level switch
    { ProgramLevel.Beginner => "Начинающий", ProgramLevel.Intermediate => "Средний", ProgramLevel.Advanced => "Продвинутый", _ => "" };
    public static readonly string[] DayNames = ["ПН", "ВТ", "СР", "ЧТ", "ПТ", "СБ", "ВС"];
    public static IEnumerable<ProgramOccurrence> Occurrences(TrainingProgram p)
    {
        if (p.StartDate is not { } start) yield break;
        // A cycle is Weeks × 7 days from the selected start, including a partial calendar week.
        for (var offset = 0; offset < p.Weeks * 7; offset++)
        {
            var date = start.AddDays(offset);
            var day = ((int)date.DayOfWeek + 6) % 7 + 1;
            foreach (var workout in p.Workouts.Where(w => w.DayOfWeek == day)) yield return new(workout, date);
        }
    }
    public static ProgramOccurrence? Next(TrainingProgram p, IReadOnlyList<WorkoutSession> sessions) =>
        Occurrences(p).FirstOrDefault(o => sessions.Any(s => s.ProgramWorkoutKey == o.Workout.Key && s.ScheduledDate == o.Date && !s.CompletedAtUtc.HasValue)) ??
        Occurrences(p).FirstOrDefault(o => o.Date >= DateOnly.FromDateTime(DateTime.UtcNow) && !sessions.Any(s => s.ProgramWorkoutKey == o.Workout.Key && s.ScheduledDate == o.Date && s.CompletedAtUtc.HasValue));
    public static int Done(TrainingProgram p, IReadOnlyList<WorkoutSession> sessions) => Occurrences(p).Count(o =>
        sessions.Any(s => s.ProgramWorkoutKey == o.Workout.Key && s.ScheduledDate == o.Date && s.CompletedAtUtc.HasValue));
    public static int CurrentWeek(TrainingProgram p) => p.StartDate is { } start
        ? Math.Clamp((DateOnly.FromDateTime(DateTime.UtcNow).DayNumber - start.DayNumber) / 7 + 1, 1, p.Weeks) : 0;
}
