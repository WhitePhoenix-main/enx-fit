using System.Globalization;
using System.Text;
using System.Text.Json;
using enx_fit.Areas.Identity.Data;
using enx_fit.Data;
using enx_fit.Security;
using enx_fit.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace enx_fit.Pages;

[MinimumRole(UserRole.User)]
public sealed class ProfileModel(UserManager<ApplicationUser> users, ApplicationDbContext db) : PageModel
{
    [BindProperty(SupportsGet = true)] public bool Reference { get; set; }
    public ApplicationUser Viewer { get; private set; } = null!;
    public string? TrainerName { get; private set; }
    public DashboardSettings? Preferences { get; private set; }
    public bool HasPreferences => Preferences?.SetupStatus == SetupStatus.Completed;
    public string DisplayName => Viewer.UserName?.Split('@')[0] is { Length: > 0 } name ? name : "Спортсмен";
    public string Initial => StringInfo.GetNextTextElement(DisplayName).ToUpperInvariant();
    public async Task<IActionResult> OnGetAsync()
    {
        if (Request.Query.ContainsKey("ClientId")) return NotFound();
        Viewer = (await users.GetUserAsync(User))!;
        if (Viewer is null) return Challenge();
        if (Viewer.TrainerId is { } trainerId) TrainerName = (await users.FindByIdAsync(trainerId))?.UserName;
        if (!Reference) Preferences = await db.DashboardSettings.AsNoTracking().SingleOrDefaultAsync(s => s.UserId == Viewer.Id);
        ViewData["Reference"] = Reference;
        return Page();
    }

    public async Task<IActionResult> OnGetExportAsync(string format = "json")
    {
        if (Request.Query.ContainsKey("ClientId")) return NotFound();
        var user = await users.GetUserAsync(User);
        if (user is null) return Challenge();
        if (format is not ("json" or "csv")) return BadRequest();
        var workouts = await db.WorkoutSessions.AsNoTracking().Where(w => w.UserId == user.Id)
            .OrderByDescending(w => w.Date).Select(w => new { w.Id, w.Date, w.Title,
                Exercises = w.WorkoutExercises.Select(e => new { Exercise = e.Exercise.Name,
                    Sets = e.SetEntries.Select(s => new { s.Weight, s.Reps, s.IsCompleted, s.IsWarmup }) }) }).ToListAsync();
        Response.Headers.CacheControl = "no-store";
        if (format == "json") return File(JsonSerializer.SerializeToUtf8Bytes(workouts, new JsonSerializerOptions { WriteIndented = true }), "application/json", "enix-fit-workouts.json");
        static string Csv(string? text)
        {
            var value = text ?? "";
            if (value.TrimStart().FirstOrDefault() is '=' or '+' or '-' or '@' || value.StartsWith('\t') || value.StartsWith('\r')) value = "'" + value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        var rows = new StringBuilder("Дата,Тренировка,Упражнение,Вес,Повторы,Выполнен,Разминка\r\n");
        foreach (var w in workouts) foreach (var e in w.Exercises) foreach (var s in e.Sets)
            rows.AppendLine($"{w.Date:yyyy-MM-dd},{Csv(w.Title)},{Csv(e.Exercise)},{s.Weight.ToString(CultureInfo.InvariantCulture)},{s.Reps},{s.IsCompleted},{s.IsWarmup}");
        return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(rows.ToString())).ToArray(), "text/csv; charset=utf-8", "enix-fit-workouts.csv");
    }
}
