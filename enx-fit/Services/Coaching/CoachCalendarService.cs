using System.ComponentModel.DataAnnotations;
using enx_fit.Data;
using enx_fit.Models;
using enx_fit.Security;
using Microsoft.EntityFrameworkCore;

namespace enx_fit.Services.Coaching;

public sealed record CoachCalendarEntry(int Id, string ClientId, string ClientName, DateOnly Date, TimeOnly Time, int Minutes, string Title, WorkoutStatus Status);
public sealed record CoachWorkoutChoice(int Id, string Title);

public sealed class CoachCalendarService(ApplicationDbContext db, CurrentUser user, IFeatureAccessService features)
{
    private IQueryable<WorkoutSession> AssignedSessions() => db.WorkoutSessions.Where(w => w.UserId != user.Id && db.Users.Any(c => c.Id == w.UserId && c.TrainerId == user.Id));
    public async Task<IReadOnlyList<CoachCalendarEntry>> WeekAsync(DateOnly monday)
    {
        if (!await features.CanUseAsync(Feature.CoachClientManagement)) return [];
        return await AssignedSessions().AsNoTracking().Where(w => w.Date >= monday && w.Date < monday.AddDays(7) && w.ScheduledTime != null)
            .OrderBy(w => w.Date).ThenBy(w => w.ScheduledTime).Select(w => new CoachCalendarEntry(w.Id, w.UserId!,
                db.Users.Where(c => c.Id == w.UserId).Select(c => c.UserName ?? "Клиент").First(), w.Date, w.ScheduledTime!.Value,
                w.PlannedMinutes ?? 60, w.Title ?? "Тренировка", w.Status)).ToListAsync();
    }
    public async Task<IReadOnlyList<CoachWorkoutChoice>> ChoicesAsync() => await db.Set<ProgramWorkout>().AsNoTracking()
        .Where(w => db.TrainingPrograms.Any(p => p.Id == w.TrainingProgramId && !p.IsArchived && (p.IsTemplate || p.OwnerId == user.Id)))
        .OrderBy(w => w.Name).Select(w => new CoachWorkoutChoice(w.Id, w.Name)).ToListAsync();

    public async Task<int?> AssignAsync(string clientId, int workoutId, DateOnly date, TimeOnly time, int minutes, Guid requestId)
    {
        if (!await features.CanUseAsync(Feature.CoachProgramAssignment)) return null;
        if (!await db.Users.AnyAsync(c => c.Id == clientId && c.TrainerId == user.Id && c.Id != user.Id)) return null;
        if (date < user.LocalToday.AddYears(-1) || date > user.LocalToday.AddYears(1) || minutes is < 15 or > 180 || requestId == Guid.Empty)
            throw new ValidationException("Проверьте дату и длительность занятия.");
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var existing = await AssignedSessions().AsNoTracking().Where(w => w.UserId == clientId && w.ClientRequestId == requestId).Select(w => (int?)w.Id).SingleOrDefaultAsync();
        if (existing.HasValue) return existing;
        var source = await db.Set<ProgramWorkout>().AsNoTracking().Include(w => w.Exercises).ThenInclude(e => e.Exercise)
            .SingleOrDefaultAsync(w => w.Id == workoutId && db.TrainingPrograms.Any(p => p.Id == w.TrainingProgramId && !p.IsArchived && (p.IsTemplate || p.OwnerId == user.Id)));
        if (source is null) return null;
        var end = time.ToTimeSpan().Add(TimeSpan.FromMinutes(minutes));
        if (end > TimeSpan.FromHours(24)) throw new ValidationException("Занятие должно завершиться в тот же день.");
        var slots = await AssignedSessions().Where(w => w.Date == date && w.ScheduledTime != null && w.Status != WorkoutStatus.Cancelled)
            .Select(w => new { Time = w.ScheduledTime!.Value, Minutes = w.PlannedMinutes ?? 60 }).ToListAsync();
        if (slots.Any(s => time.ToTimeSpan() < s.Time.ToTimeSpan().Add(TimeSpan.FromMinutes(s.Minutes)) && end > s.Time.ToTimeSpan()))
            throw new ValidationException("На это время уже назначено занятие. Выберите свободный интервал.");
        var session = new WorkoutSession { UserId = clientId, Date = date, ScheduledTime = time, PlannedMinutes = minutes,
            Title = source.Name, Status = WorkoutStatus.Planned, ClientRequestId = requestId,
            WorkoutExercises = source.Exercises.OrderBy(e => e.Order).Select(e => new WorkoutExercise { ExerciseId = e.ExerciseId, Order = e.Order,
                TargetSets = e.Prescription.Sets, TargetRepsMin = e.Prescription.RepsMin, TargetRepsMax = e.Prescription.RepsMax,
                TargetRestSeconds = e.Prescription.RestSeconds, TargetWeightKg = e.Prescription.WeightKg }).ToList() };
        db.WorkoutSessions.Add(session); await db.SaveChangesAsync(); await transaction.CommitAsync(); return session.Id;
    }
    public async Task<bool> CancelAsync(int id)
    {
        if (!await features.CanUseAsync(Feature.CoachProgramAssignment)) return false;
        return await AssignedSessions().Where(w => w.Id == id && w.Status == WorkoutStatus.Planned && w.ScheduledTime != null)
            .ExecuteUpdateAsync(u => u.SetProperty(w => w.Status, WorkoutStatus.Cancelled).SetProperty(w => w.Revision, Guid.NewGuid())) == 1;
    }
}
