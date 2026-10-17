using enx_fit.Areas.Identity.Data;
using enx_fit.Data;
using enx_fit.Models;
using enx_fit.Security;
using enx_fit.Services.Workouts;
using Microsoft.EntityFrameworkCore;

namespace enx_fit.Services.Dashboard;

public sealed class DashboardService(ApplicationDbContext db, CurrentUser currentUser, UserAccountReader accounts)
{
    private Task<IReadOnlyList<ClientSummary>>? assignedClients;

    public async Task<string> GetGoalTitleAsync() =>
        await db.DashboardSettings.AsNoTracking()
            .Where(settings => settings.UserId == currentUser.Id)
            .Select(settings => settings.GoalTitle)
            .SingleOrDefaultAsync() ?? "Тренироваться регулярно";

    // Resolve the subject before reading any of their fitness data, on every request.
    public Task<ApplicationUser?> GetSubjectAsync(string? clientId)
    {
        var id = string.IsNullOrWhiteSpace(clientId) ? currentUser.Id : clientId;
        if (id == currentUser.Id) return accounts.GetAsync(id);
        return db.Users.AsNoTracking().SingleOrDefaultAsync(user => user.Id == id &&
            (user.Id == currentUser.Id || currentUser.IsAdministrator ||
             ((currentUser.Role == UserRole.Trainer || currentUser.IsAdministrator) && user.TrainerId == currentUser.Id)));
    }

    public Task<IReadOnlyList<ClientSummary>> GetClientsAsync(string? search = null) =>
        string.IsNullOrWhiteSpace(search)
            ? assignedClients ??= LoadClientsAsync(null)
            : LoadClientsAsync(search);

    private async Task<IReadOnlyList<ClientSummary>> LoadClientsAsync(string? search)
    {
        if (currentUser.Role != UserRole.Trainer && !currentUser.IsAdministrator) return [];
        var users = db.Users.AsNoTracking().Where(user => user.TrainerId == currentUser.Id);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToUpperInvariant();
            users = users.Where(user => (user.NormalizedUserName ?? "").Contains(term) ||
                                       (user.NormalizedEmail ?? "").Contains(term));
        }
        var today = currentUser.LocalToday;
        var completed = db.WorkoutSessions.Where(WorkoutStates.Completed)
            .Where(workout => workout.Date <= today &&
                workout.WorkoutExercises.Any(exercise =>
                    exercise.SetEntries.Any(set => set.IsCompleted && !set.IsWarmup && set.Reps > 0)));
        return await users.OrderBy(user => user.UserName).Select(user => new ClientSummary(
            user.Id, user.UserName ?? user.Email ?? "Клиент",
            completed.Count(workout => workout.UserId == user.Id),
            completed.Where(workout => workout.UserId == user.Id)
                .Select(workout => (DateOnly?)workout.Date).Max())).ToListAsync();
    }

    public async Task<DashboardData> LoadAsync(ApplicationUser subject, int days, DateOnly until)
    {
        var today = currentUser.LocalToday;
        var data = new DashboardData { Subject = subject, Today = today, Until = until, Days = days };
        data.Settings = await db.DashboardSettings.AsNoTracking().SingleOrDefaultAsync(s => s.UserId == subject.Id)
                        ?? new DashboardSettings { UserId = subject.Id };
        data.Workouts = await db.WorkoutSessions.AsNoTracking().AsSplitQuery().Where(w => w.UserId == subject.Id)
            .Include(w => w.WorkoutExercises).ThenInclude(e => e.Exercise)
            .Include(w => w.WorkoutExercises).ThenInclude(e => e.SetEntries)
            .OrderByDescending(w => w.Date).ThenByDescending(w => w.Id).ToListAsync();
        data.Measurements = await db.BodyMeasurements.AsNoTracking()
            .Where(m => m.UserId == subject.Id && m.Date <= until).OrderBy(m => m.Date).ThenBy(m => m.Id).ToListAsync();
        data.CheckIns = await db.DailyCheckIns.AsNoTracking()
            .Where(c => c.UserId == subject.Id && c.Date >= today.AddDays(-6) && c.Date <= today)
            .OrderBy(c => c.Date).ToListAsync();
        return data;
    }
}
