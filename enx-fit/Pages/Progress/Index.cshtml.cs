using enx_fit.Security;
using enx_fit.Services.Analytics;
using enx_fit.ViewModels.Progress;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace enx_fit.Pages.Progress;

[MinimumRole(UserRole.User)]
public sealed class IndexModel(ProgressOverviewQuery query, enx_fit.Data.ApplicationDbContext db) : PageModel
{
    public int TotalSeconds { get; private set; }
    public int? AverageCalories { get; private set; }
    public IReadOnlyList<(int Id, DateOnly Date)> Photos { get; private set; } = [];
    public string? TrainerNote { get; private set; }
    [BindProperty(SupportsGet=true)] public int Weeks { get; set; }
    [BindProperty(SupportsGet=true)] public string? ClientId { get; set; }
    [BindProperty(SupportsGet=true)] public string? SelectedClient { get; set; }
    [BindProperty(SupportsGet=true)] public int? ProgramId { get; set; }
    [BindProperty(SupportsGet=true)] public Guid? ExerciseId { get; set; }
    [BindProperty(SupportsGet=true)] public Guid? ChartExercise { get; set; }
    [BindProperty(SupportsGet=true)] public string Metric { get; set; } = "weight";
    [BindProperty(SupportsGet=true)] public string ClientMetric { get; set; } = "weight";
    [BindProperty(SupportsGet=true)] public bool Personal { get; set; }
    public ProgressOverview Data { get; private set; } = null!;
    public IReadOnlyList<(string Key,string Label)> Metrics => new[] {("weight","Рабочий вес"),("volume","Объём"),("1rm","Оценка 1RM"),("reps","Повторения")}
        .Where(m=>m.Item1=="weight"||(m.Item1=="volume"?Data.Access.CanUse(Feature.AnalyticsTrainingVolume):Data.Advanced)).ToArray();
    public async Task<IActionResult> OnGetAsync()
    {
        // Keep links from the previous progress workspace usable.
        if (Weeks==0 && int.TryParse(Request.Query["ProgressWeeks"],out var oldWeeks)) Weeks=oldWeeks;
        if (ChartExercise is null && Guid.TryParse(Request.Query["ProgressExercise"],out var oldExercise)) ChartExercise=oldExercise;
        if (ClientId == "") ClientId=null;
        if (SelectedClient == "") SelectedClient=null;
        ClientMetric=ClientMetric is "strength" or "measurements" or "attendance" ? ClientMetric : "weight";
        Data=(await query.LoadAsync(Weeks,ClientId,ProgramId,ExerciseId,ChartExercise,Metric,ClientMetric,Personal,SelectedClient))!;
        if (Data is null) return NotFound();
        if (Data.Person is { } person)
        {
            var photos = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(db.ProgressPhotos.AsNoTracking()
                .Where(p => p.UserId == person.Client.Id).OrderBy(p => p.Date).ThenBy(p => p.Id).Select(p => new { p.Id, p.Date }));
            Photos = photos.Select(p => (p.Id, p.Date)).ToArray();
            TrainerNote = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(db.DashboardSettings
                .Where(s => s.UserId == person.Client.Id).Select(s => s.TrainerNote));
            var sessions = db.WorkoutSessions.Where(enx_fit.Services.Workouts.WorkoutStates.Completed)
                .Where(w => w.UserId == person.Client.Id && w.Date >= Data.Start && w.Date <= Data.Today);
            TotalSeconds = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.SumAsync(sessions, w => w.DurationSeconds ?? 0);
            var days = db.MealEntries.Where(m => m.UserId == person.Client.Id && m.Date >= Data.Start && m.Date <= Data.Today)
                .GroupBy(m => m.Date).Select(g => g.Sum(m => m.Calories));
            var totals = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(days);
            AverageCalories = totals.Count > 0 ? (int)Math.Round(totals.Average()) : null;
        }
        Weeks=Data.Weeks; Metric=Data.Person?.Metric??"weight"; ChartExercise=Data.Person?.ExerciseId;
        if (!Data.Exercises.Any(e=>e.Id==ExerciseId?.ToString())) ExerciseId=null;
        Response.Headers.CacheControl="no-cache, no-store";
        return Page();
    }
    public async Task<IActionResult> OnPostNoteAsync(string clientId, string? note, [FromServices] CurrentUser user, [FromServices] IFeatureAccessService features)
    {
        if (!await features.CanUseAsync(Feature.CoachAnalytics) || !await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(db.Users.Where(c => c.Id == clientId && c.TrainerId == user.Id))) return NotFound();
        if ((note?.Length ?? 0) > 1000) return BadRequest();
        var settings = await db.DashboardSettings.FindAsync(clientId);
        if (settings is null) { settings = new enx_fit.Models.DashboardSettings { UserId = clientId }; db.DashboardSettings.Add(settings); }
        settings.TrainerNote = note?.Trim(); await db.SaveChangesAsync();
        return RedirectToPage(new { SelectedClient = clientId, Weeks });
    }
    public object Values(string? metric=null,string? clientMetric=null) => new { Weeks,ClientId,ProgramId,ExerciseId,ChartExercise,SelectedClient,Metric=metric??Metric,ClientMetric=clientMetric??ClientMetric,Personal=Personal?(bool?)true:null };
}
