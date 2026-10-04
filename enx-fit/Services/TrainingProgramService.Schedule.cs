using System.Data;
using enx_fit.Models;
using Microsoft.EntityFrameworkCore;

namespace enx_fit.Services;

public sealed partial class TrainingProgramService
{
    public async Task MoveOccurrenceAsync(int id, Guid workoutKey, DateOnly originalDate, DateOnly date, Guid revision)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var program = await EditableAsync(id);
        Require(program.OwnerId == currentUser.Id, ProgramFailure.Forbidden);
        var occurrence = ProgramSchedule.Occurrences(program).SingleOrDefault(o => o.Workout.Key == workoutKey && o.SlotDate == originalDate);
        Require(occurrence is not null, ProgramFailure.NotFound);
        var sessions = await SessionsAsync(program);
        Require(!sessions.Any(s => ProgramSchedule.Matches(s, occurrence!) && (s.IsActive || s.IsFinished)),
            ProgramFailure.Invalid, "Начатое или завершённое занятие нельзя перенести. Его история сохранена.");
        Require(date >= currentUser.LocalToday && date >= program.StartDate!.Value && date < program.StartDate.Value.AddDays(program.Weeks * 7),
            ProgramFailure.Invalid, "Выберите сегодняшний или будущий день в пределах этой программы.");
        // A retry with an already applied destination is a no-op, even after a lost response.
        if (occurrence!.Date == date) return;
        Require(program.Revision == revision, ProgramFailure.Conflict);
        Require(!ProgramSchedule.Occurrences(program).Any(o => o.Date == date &&
            (o.Workout.Key != workoutKey || o.SlotDate != originalDate)), ProgramFailure.Invalid,
            "На этот день уже назначена другая тренировка этой программы. Выберите свободный день.");
        var change = program.ScheduleChanges.SingleOrDefault(c => c.ProgramWorkoutKey == workoutKey && c.OriginalDate == originalDate);
        if (date == originalDate)
        {
            if (change is not null) db.Remove(change);
        }
        else if (change is not null) change.Date = date;
        else program.ScheduleChanges.Add(new() { ProgramWorkoutKey = workoutKey, OriginalDate = originalDate, Date = date });
        // A separately prepared session uses its original slot, but follows the new visible date.
        foreach (var planned in await db.WorkoutSessions.Where(s => s.TrainingProgramId == id && s.UserId == currentUser.Id &&
            s.ProgramWorkoutKey == workoutKey && s.ScheduledDate == originalDate).ToListAsync())
            if (planned.State == WorkoutStatus.Planned) { planned.Date = date; planned.Revision = Guid.NewGuid(); }
        program.Revision = Guid.NewGuid();
        await db.SaveChangesAsync(); await transaction.CommitAsync();
    }

    public async Task<int> StartOccurrenceAsync(int id, Guid workoutKey, DateOnly originalDate)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var program = await EditableAsync(id);
        Require(program.OwnerId == currentUser.Id, ProgramFailure.Forbidden);
        var occurrence = ProgramSchedule.Occurrences(program).SingleOrDefault(o => o.Workout.Key == workoutKey && o.SlotDate == originalDate);
        Require(occurrence is not null && occurrence.Workout.Exercises.Count > 0, ProgramFailure.NotFound);
        var sessions = await SessionsAsync(program);
        var matching = sessions.Where(s => ProgramSchedule.Matches(s, occurrence!)).ToList();
        Require(!matching.Any(s => s.State == WorkoutStatus.Completed), ProgramFailure.Invalid, "Это занятие уже выполнено. Результаты доступны в истории.");
        var active = await db.WorkoutSessions.Where(s => s.UserId == currentUser.Id).Where(WorkoutStates.Active).FirstOrDefaultAsync();
        if (active is not null) return active.Id;
        var preparedId = matching.FirstOrDefault(s => s.State == WorkoutStatus.Planned)?.Id;
        var session = preparedId.HasValue ? await db.WorkoutSessions.Include(s => s.WorkoutExercises).ThenInclude(e => e.SetEntries).SingleAsync(s => s.Id == preparedId) : new WorkoutSession
        {
            UserId = currentUser.Id, Title = occurrence!.Workout.Name, TrainingProgramId = id,
            ProgramWorkoutKey = workoutKey, ScheduledDate = originalDate,
            WorkoutExercises = WorkoutService.CopyPrescription(occurrence.Workout)
        };
        session.StartedAtUtc = DateTime.UtcNow; session.Status = WorkoutStatus.InProgress;
        session.Date = currentUser.LocalToday; session.UtcOffsetMinutes = currentUser.UtcOffsetMinutes; session.Revision = Guid.NewGuid();
        if (!preparedId.HasValue) db.Add(session);
        try { await db.SaveChangesAsync(); await transaction.CommitAsync(); return session.Id; }
        catch (Exception ex) when (ex is DbUpdateException or System.Data.Common.DbException)
        {
            await transaction.RollbackAsync(); db.ChangeTracker.Clear();
            var existing = await db.WorkoutSessions.AsNoTracking().Where(s => s.UserId == currentUser.Id).Where(WorkoutStates.Active).FirstOrDefaultAsync();
            if (existing is not null) return existing.Id;
            throw new ProgramOperationException(ProgramFailure.Conflict, "Повторите запуск тренировки.");
        }
    }
}
