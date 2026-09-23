using enx_fit.Data;
using enx_fit.Models;
using enx_fit.Security;
using Microsoft.EntityFrameworkCore;

namespace enx_fit.Services;

public sealed record ClientAttention(string ClientId, string Name, int ProgramId, string Reason);
public sealed record CoachAttention(int ActiveClients, int ScheduledToday, int CompletedToday, IReadOnlyList<ClientAttention> Alerts);

public sealed class CoachAttentionService(ApplicationDbContext db, CurrentUser user, IFeatureAccessService features,
    IProgramAnalysisService analysis, IPlateauDetectionService plateau)
{
    public async Task<CoachAttention> LoadAsync(string? clientId = null)
    {
        if (!await features.CanUseAsync(Feature.CoachClientAlerts)) throw new ProgramOperationException(ProgramFailure.Forbidden);
        var clients = await db.Users.AsNoTracking().Where(u => u.Id != user.Id &&
            (user.IsAdministrator || u.TrainerId == user.Id) && (clientId == null || u.Id == clientId))
            .Select(u => new { u.Id, Name = u.UserName ?? "Клиент" }).ToDictionaryAsync(u => u.Id);
        var ids = clients.Keys.ToArray();
        var programs = await db.TrainingPrograms.AsNoTracking().AsSplitQuery()
            .Include(p => p.Assignment).Include(p => p.Workouts).ThenInclude(w => w.Exercises).ThenInclude(e => e.Exercise)
            .Where(p => !p.IsArchived && p.OwnerId != null && ids.Contains(p.OwnerId) && p.Assignment != null &&
                (user.IsAdministrator || p.Assignment.TrainerId == user.Id)).ToListAsync();
        var programIds = programs.Select(p => p.Id).ToArray();
        var sessions = await db.WorkoutSessions.AsNoTracking().AsSplitQuery().Include(s => s.WorkoutExercises).ThenInclude(e => e.SetEntries)
            .Where(s => s.TrainingProgramId.HasValue && programIds.Contains(s.TrainingProgramId.Value) && s.UserId != null && ids.Contains(s.UserId)).ToListAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var alerts = new List<ClientAttention>();
        var scheduled = 0; var completed = 0;
        foreach (var p in programs)
        {
            var history = sessions.Where(s => s.TrainingProgramId == p.Id && s.UserId == p.OwnerId).ToList();
            var completion = analysis.Completion(p, history, today);
            var todays = ProgramSchedule.Occurrences(p).Where(o => o.Date == today).ToList();
            scheduled += todays.Count;
            completed += todays.Count(o => history.Any(s => s.ProgramWorkoutKey == o.Workout.Key && s.ScheduledDate == today && s.CompletedAtUtc.HasValue));
            void Add(string reason) => alerts.Add(new(p.OwnerId!, clients[p.OwnerId!].Name, p.Id, reason));
            if (completion.Missed >= 2) Add($"{completion.Missed} пропущенных тренировок в программе «{p.Name}».");
            if (completion.Ends is { } end && end >= today && end <= today.AddDays(7)) Add($"Программа «{p.Name}» заканчивается через {end.DayNumber - today.DayNumber} дней.");
            foreach (var workout in p.Workouts)
            foreach (var exercise in workout.Exercises)
            {
                var performances = history.Where(s => s.CompletedAtUtc.HasValue && s.Date <= today && s.ProgramWorkoutKey == workout.Key)
                    .OrderByDescending(s => s.Date).ThenByDescending(s => s.Id)
                    .SelectMany(s => s.WorkoutExercises.Where(e => e.ExerciseId == exercise.ExerciseId && e.SetEntries.Any(x => !x.IsWarmup && x.Reps > 0))
                        .Select(e => new ExercisePerformance(s.Id, s.Date,
                            e.SetEntries.Where(x => !x.IsWarmup && x.Reps > 0).OrderBy(x => x.SetNumber).Select(x => new PerformedSet(x.SetNumber, x.Weight, x.Reps, x.Rir)).ToList(),
                            e.TargetSets, e.TargetRepsMin, e.TargetRepsMax, e.TargetRir, e.TargetRpe))).Take(4).ToList();
                if (plateau.Analyze(exercise, performances) is not null) Add($"{exercise.Exercise.Name}: возможное плато в последних 4 сопоставимых тренировках.");
                if (performances.Count == 4 && performances[0].Date >= today.AddDays(-21) && performances.All(h =>
                    h.Rir.HasValue && h.Sets.All(s => s.Rir.HasValue) && h.Sets.Average(s => s.Rir!.Value) < h.Rir.Value))
                    Add($"{exercise.Exercise.Name}: средний RIR ниже заданного 4 тренировки подряд.");
            }
        }
        return new(clients.Count, scheduled, completed, alerts);
    }
}
