using enx_fit.Data;
using enx_fit.Models;
using enx_fit.Security;
using enx_fit.ViewModels.Dashboard;

namespace enx_fit.Services.Dashboard;

// Internal commands accept only a subject resolved by DashboardService for this request.
public sealed class DashboardCommands(ApplicationDbContext db, CurrentUser user)
{
    internal async Task SaveGoalAsync(string subjectId, GoalInput input)
    {
        var settings = await db.DashboardSettings.FindAsync(subjectId);
        if (settings is null) { settings = new DashboardSettings { UserId = subjectId }; db.DashboardSettings.Add(settings); }
        settings.GoalTitle = input.Title.Trim();
        settings.WeeklyWorkoutGoal = input.WeeklyWorkouts;
        settings.PreferencesRevision = Guid.NewGuid();
        if (subjectId != user.Id) settings.TrainerNote = input.TrainerNote?.Trim();
        await db.SaveChangesAsync();
    }

    internal async Task SaveCheckInAsync(CheckInInput input)
    {
        var today = user.LocalToday;
        var entry = await db.DailyCheckIns.FindAsync(user.Id, today);
        if (entry is null) { entry = new DailyCheckIn { UserId = user.Id, Date = today }; db.DailyCheckIns.Add(entry); }
        entry.WaterMl = input.WaterMl; entry.Steps = input.Steps; entry.SleepMinutes = input.SleepMinutes;
        entry.ReadingMinutes = input.ReadingMinutes;
        entry.NutritionLogged = input.NutritionLogged; entry.StretchingDone = input.StretchingDone;
        await db.SaveChangesAsync();
    }
}
