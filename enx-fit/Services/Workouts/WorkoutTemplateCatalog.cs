using enx_fit.Data;
using enx_fit.Models;
using enx_fit.Presentation.Programs;
using enx_fit.Security;
using enx_fit.Services.Programs;
using enx_fit.ViewModels.Programs;
using enx_fit.ViewModels.Workouts;
using Microsoft.EntityFrameworkCore;

namespace enx_fit.Services.Workouts;

public sealed class WorkoutTemplateCatalog(ApplicationDbContext db, CurrentUser user, TrainingProgramService programs)
{
    public Task<List<WorkoutTemplateCollection>> CollectionsAsync() => db.Set<WorkoutTemplateCollection>().AsNoTracking().Where(c => c.UserId == user.Id).OrderBy(c => c.Name).ToListAsync();
    public async Task SaveCollectionAsync(string name, IReadOnlyList<int> workoutIds)
    {
        if ((await programs.AccessAsync()).Plan == SubscriptionPlan.Free) throw new UnauthorizedAccessException();
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 60 || workoutIds.Count > 80) throw new ArgumentException("Название коллекции: до 60 символов; не более 80 шаблонов.");
        var available = (await ListAsync()).Select(c => c.Workout.Id).ToHashSet();
        if (workoutIds.Any(id => !available.Contains(id))) throw new KeyNotFoundException();
        db.Add(new WorkoutTemplateCollection { UserId = user.Id, Name = name.Trim(), WorkoutIds = workoutIds.Distinct().ToArray() });
        await db.SaveChangesAsync();
    }
    public async Task<bool> DeleteCollectionAsync(int id)
    {
        if ((await programs.AccessAsync()).Plan == SubscriptionPlan.Free) return false;
        return await db.Set<WorkoutTemplateCollection>().Where(c => c.Id == id && c.UserId == user.Id).ExecuteDeleteAsync() == 1;
    }
    public async Task<IReadOnlyList<WorkoutTemplateCard>> ListAsync()
    {
        var items = await db.TrainingPrograms.AsNoTracking().AsSplitQuery()
            .Where(p => !p.IsArchived && (p.IsTemplate || p.OwnerId == user.Id))
            .Include(p => p.Workouts).ThenInclude(w => w.Exercises).ThenInclude(e => e.Exercise).ToListAsync();
        var preferences = await db.DashboardSettings.AsNoTracking().SingleOrDefaultAsync(s => s.UserId == user.Id);
        var usage = await db.WorkoutSessions.AsNoTracking().Where(s => s.UserId == user.Id && s.SourceProgramWorkoutId != null && s.Date <= user.LocalToday)
            .Where(WorkoutStates.Completed).GroupBy(s => s.SourceProgramWorkoutId!.Value).Select(g => new { Id=g.Key, Count=g.Count() }).ToDictionaryAsync(g => g.Id,g => g.Count);
        return items.SelectMany(p => p.Workouts.Where(w => w.Exercises.Count > 0).Select(w =>
            new WorkoutTemplateCard(p,w,p.OwnerId == user.Id,usage.GetValueOrDefault(w.Id),ProgramDiscovery.Reasons(p,preferences))))
            .OrderByDescending(c => c.Reasons.Count).ThenByDescending(c => c.Uses).ThenBy(c => c.Title).ToArray();
    }

    public async Task<int> CopyAsync(int workoutId)
    {
        var card = (await ListAsync()).SingleOrDefault(c => c.Workout.Id == workoutId) ?? throw new KeyNotFoundException();
        var name = card.Title[..Math.Min(card.Title.Length,108)] + " · копия";
        // A copy is one independent prescription; results and the original program are untouched.
        return await programs.SaveAsync(null,new ProgramInput { Name=name, Goal=card.Goal, Description=card.Program.Description,
            Level=card.Program.Level, Weeks=1, DaysPerWeek=1, Workouts=[new() { Name=name, EstimatedMinutes=card.Workout.EstimatedMinutes,
                Exercises=card.Exercises.Select(e => new ProgramExerciseInput { ExerciseId=e.ExerciseId, Prescription=new() {
                    Sets=e.Prescription.Sets, RepsMin=e.Prescription.RepsMin, RepsMax=e.Prescription.RepsMax,
                    WeightKg=e.Prescription.WeightKg, PercentOneRepMax=e.Prescription.PercentOneRepMax,
                    Rir=e.Prescription.Rir,Rpe=e.Prescription.Rpe,RestSeconds=e.Prescription.RestSeconds,Comment=e.Prescription.Comment } }).ToList() }] });
    }
}
