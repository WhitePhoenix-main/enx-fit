using enx_fit.Data;
using enx_fit.Models;
using enx_fit.Security;
using enx_fit.Services.Programs;
using enx_fit.Services.Workouts;
using Microsoft.EntityFrameworkCore;

namespace enx_fit.Services.Dashboard;

public sealed record DashboardProgramCard(int Id, string Name, int Weeks, int DaysPerWeek,
    ProgramLevel Level, bool RequiresPro, string Categories);
public sealed record CoachRecordCard(string ClientId, string ClientName, string Exercise, decimal Weight, decimal? Gain);
public sealed record CoachMessagePreview(string ClientId, string ClientName, string Text, DateTime CreatedAtUtc);
public sealed record CoachScheduleCard(string ClientId, string ClientName, string Workout, int ProgramId, TimeOnly? Time = null);
public sealed record CoachOverview(IReadOnlyList<DashboardDay> Days, IReadOnlyList<CoachRecordCard> Records,
    IReadOnlyList<CoachScheduleCard> Schedule, int TrainedToday = 0, int ActivePrograms = 0, int CompletionPercent = 0, int ScheduledTotal = 0,
    IReadOnlyList<CoachMessagePreview>? Messages = null, IReadOnlyList<DashboardDay>? WeekPlans = null);

// All coach aggregates are scoped to the current trainer's assigned clients, including for admins.
public sealed class SubscriptionDashboardQuery(ApplicationDbContext db, CurrentUser currentUser, IFeatureAccessService features)
{
    public async Task<IReadOnlyList<DashboardProgramCard>> ProgramsAsync(bool coach)
    {
        var access = await features.GetAsync();
        if (coach && !access.CanUse(Feature.CoachProgramAssignment)) throw new ProgramOperationException(ProgramFailure.Forbidden);
        return await db.TrainingPrograms.AsNoTracking()
            .Where(p => !p.IsArchived && (coach ? p.OwnerId == currentUser.Id && p.Assignment == null : p.IsTemplate))
            .Where(p => coach || access.CanUse(Feature.ProgramAutoProgression) || !p.RequiresPro)
            .OrderBy(p => p.Level).ThenBy(p => p.Name).Take(coach ? 4 : 3)
            .Select(p => new DashboardProgramCard(p.Id, p.Name, p.Weeks, p.DaysPerWeek, p.Level, p.RequiresPro, p.Categories))
            .ToListAsync();
    }

    public async Task<CoachOverview> CoachAsync(DateOnly today, bool fullSchedule = false)
    {
        if (!await features.CanUseAsync(Feature.CoachClientManagement)) throw new ProgramOperationException(ProgramFailure.Forbidden);
        var clients = db.Users.AsNoTracking().Where(u => u.TrainerId == currentUser.Id && u.Id != currentUser.Id);
        var sessions = db.WorkoutSessions.AsNoTracking().Where(w => clients.Any(c => c.Id == w.UserId) && w.Date <= today &&
            w.WorkoutExercises.Any(e => e.SetEntries.Any(s => s.IsCompleted && !s.IsWarmup && s.Reps > 0))).Where(WorkoutStates.Completed);
        var since = today.AddDays(-41);
        var days = await sessions.Where(w => w.Date >= since).GroupBy(w => w.Date)
            .Select(g => new DashboardDay(g.Key, g.Count(), 0)).ToListAsync();
        var sets = sessions.SelectMany(w => w.WorkoutExercises.SelectMany(e => e.SetEntries
            .Where(s => s.IsCompleted && !s.IsWarmup && s.Reps > 0 && s.Weight > 0)
            .Select(s => new { w.UserId, w.Date, e.ExerciseId, e.Exercise.Name, s.Weight })));
        var records = await (from s in sets
            join c in clients on s.UserId equals c.Id
            group new { s.Date, s.Weight } by new { c.Id, ClientName = c.UserName ?? "Клиент", s.ExerciseId, s.Name } into g
            select new { g.Key.Id, g.Key.ClientName, g.Key.Name, Best = g.Max(s => s.Weight),
                Prior = g.Where(s => s.Date < since).Select(s => s.Weight).DefaultIfEmpty().Max() })
            .OrderByDescending(r => r.Best - r.Prior).Take(4).ToListAsync();
        var plans = await db.TrainingPrograms.AsNoTracking().AsSplitQuery()
            .Where(p => !p.IsArchived && p.StartDate.HasValue && p.Assignment != null && p.Assignment.TrainerId == currentUser.Id && clients.Any(c => c.Id == p.OwnerId))
            .Include(p => p.ScheduleChanges).Include(p => p.Workouts).ToListAsync();
        var names = await clients.Select(c => new { c.Id, Name = c.UserName ?? "Клиент" }).ToDictionaryAsync(c => c.Id, c => c.Name);
        var schedule = plans.SelectMany(p => ProgramSchedule.Occurrences(p).Where(o => o.Date == today)
            .Select(o => new CoachScheduleCard(p.OwnerId!, names[p.OwnerId!], o.Workout.Name, p.Id)))
            .ToList();
        var monday = today.AddDays(-((int)today.DayOfWeek + 6) % 7);
        var slots = await db.WorkoutSessions.AsNoTracking().Where(w => clients.Any(c => c.Id == w.UserId) && w.Date >= monday && w.Date < monday.AddDays(7) && w.ScheduledTime != null && w.Status != WorkoutStatus.Cancelled)
            .Select(w => new { w.UserId, w.Title, w.Date, w.ScheduledTime }).ToListAsync();
        schedule.AddRange(slots.Where(w => w.Date == today).Select(w => new CoachScheduleCard(w.UserId!, names[w.UserId!], w.Title ?? "Тренировка", 0, w.ScheduledTime)));
        var totalScheduled = schedule.Count;
        schedule = schedule.OrderBy(s => s.Time ?? TimeOnly.MaxValue).ThenBy(s => s.ClientName).Take(fullSchedule ? int.MaxValue : 6).ToList();
        var completedSlots = await sessions.Where(w => w.TrainingProgramId != null).Select(w => new { w.TrainingProgramId, w.ProgramWorkoutKey, w.ScheduledDate }).ToListAsync();
        var dueSlots = plans.SelectMany(p => ProgramSchedule.Occurrences(p).Where(o => o.Date <= today).Select(o => new { Program = p.Id, Occurrence = o })).ToList();
        var completedDue = dueSlots.Count(o => completedSlots.Any(w => w.TrainingProgramId == o.Program && w.ProgramWorkoutKey == o.Occurrence.Workout.Key && w.ScheduledDate == o.Occurrence.SlotDate));
        var trainedToday = await sessions.Where(s => s.Date == today).Select(s => s.UserId).Distinct().CountAsync();
        var messages = await db.Set<CoachMessage>().AsNoTracking().Where(m => m.RecipientId == currentUser.Id && clients.Any(c => c.Id == m.SenderId))
            .OrderByDescending(m => m.CreatedAtUtc).Take(3).Select(m => new CoachMessagePreview(m.SenderId, "", m.Text, m.CreatedAtUtc)).ToListAsync();
        var programDays = plans.SelectMany(ProgramSchedule.Occurrences).Where(o => o.Date >= monday && o.Date < monday.AddDays(7)).ToList();
        var weekPlans = Enumerable.Range(0, 7).Select(i => new DashboardDay(monday.AddDays(i), programDays.Count(o => o.Date == monday.AddDays(i)) + slots.Count(w => w.Date == monday.AddDays(i)), 0)).ToArray();
        return new(days, records.Select(r => new CoachRecordCard(r.Id, r.ClientName, r.Name, r.Best,
            r.Prior > 0 && r.Best > r.Prior ? r.Best - r.Prior : null)).ToList(), schedule, trainedToday,
            plans.Count(p => p.StartDate <= today && p.StartDate!.Value.AddDays(p.Weeks * 7) > today),
            dueSlots.Count > 0 ? completedDue * 100 / dueSlots.Count : 0, totalScheduled,
            messages.Select(m => m with { ClientName = names[m.ClientId] }).ToArray(), weekPlans);
    }
}
