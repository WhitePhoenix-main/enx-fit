using System.Text.Json;
using enx_fit.Data;
using enx_fit.Models;
using enx_fit.Security;
using enx_fit.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace enx_fit.Pages.Admin;

[MinimumRole(UserRole.Administrator)]
public class IndexModel(ApplicationDbContext db, UserDirectoryService directory) : PageModel
{
    [BindProperty(SupportsGet = true)] public int Days { get; set; } = 30;
    public int UserCount { get; private set; }
    public int WorkoutCount { get; private set; }
    public int ExerciseCount { get; private set; }
    public int MeasurementCount { get; private set; }
    public int PeriodWorkouts { get; private set; }
    public int ActiveUsers { get; private set; }
    public int PreviousWorkouts { get; private set; }
    public string ChartJson { get; private set; } = "{}";
    public IReadOnlyList<WorkoutSession> RecentWorkouts { get; private set; } = [];
    public IReadOnlyList<PopularExercise> PopularExercises { get; private set; } = [];
    public IReadOnlyDictionary<string, string> OwnerNames { get; private set; } = new Dictionary<string, string>();
    public DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

    public async Task OnGetAsync()
    {
        Days = Days is 7 or 30 or 90 ? Days : 30;
        var start = Today.AddDays(1 - Days);
        var previousStart = start.AddDays(-Days);
        UserCount = await db.Users.CountAsync();
        WorkoutCount = await db.WorkoutSessions.CountAsync();
        ExerciseCount = await db.Exercises.CountAsync();
        MeasurementCount = await db.BodyMeasurements.CountAsync();
        var period = db.WorkoutSessions.Where(w => w.Date >= start && w.Date <= Today);
        PeriodWorkouts = await period.CountAsync();
        ActiveUsers = await period.Where(w => w.UserId != null).Select(w => w.UserId).Distinct().CountAsync();
        PreviousWorkouts = await db.WorkoutSessions.CountAsync(w => w.Date >= previousStart && w.Date < start);
        var daily = await period.GroupBy(w => w.Date).Select(g => new { Date = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Date, g => g.Count);
        var dates = Enumerable.Range(0, Days).Select(i => start.AddDays(i)).ToArray();
        ChartJson = JsonSerializer.Serialize(new
        {
            labels = dates.Select(d => d.ToString("dd.MM")), values = dates.Select(d => daily.GetValueOrDefault(d))
        });
        RecentWorkouts = await db.WorkoutSessions.AsNoTracking().Include(w => w.WorkoutExercises)
            .OrderByDescending(w => w.CreatedAtUtc).ThenByDescending(w => w.Id).Take(5).ToListAsync();
        OwnerNames = await directory.GetDisplayNamesAsync(RecentWorkouts.Select(w => w.UserId));
        PopularExercises = await db.Exercises.AsNoTracking()
            .Select(e => new
            {
                e.Name, e.MuscleGroup,
                Count = e.WorkoutExercises.Count(w => w.WorkoutSession.Date >= start && w.WorkoutSession.Date <= Today)
            })
            .Where(e => e.Count > 0)
            .OrderByDescending(e => e.Count).ThenBy(e => e.Name).Take(4)
            .Select(e => new PopularExercise(e.Name, e.MuscleGroup, e.Count)).ToListAsync();
    }

    public string OwnerName(string? id) =>
        id is null ? "Не назначен" : OwnerNames.GetValueOrDefault(id, "Удалённый пользователь");
}

public sealed record PopularExercise(string Name, string MuscleGroup, int Count);
