using enx_fit.Data;
using enx_fit.Models;
using enx_fit.Security;
using enx_fit.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace enx_fit.Pages;

[MinimumRole(UserRole.User)]
[RequestSizeLimit(8192)]
public sealed class OnboardingModel(ApplicationDbContext db, CurrentUser user) : PageModel
{
    [BindProperty] public TrainingSetupInput Input { get; set; } = new();
    [BindProperty(SupportsGet = true)] public string? ReturnTo { get; set; }
    [BindProperty(SupportsGet = true)] public bool Saved { get; set; }
    public DashboardSettings Settings { get; private set; } = new();
    public string ViewerId => user.Id;
    public bool ShowSaved => Saved && Settings.SetupStatus == SetupStatus.Completed;
    public string ReturnPage => ReturnTo == "profile" ? "/Profile" : "/Dashboard";
    public bool Conflict { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (Request.Query.ContainsKey("ClientId")) return NotFound();
        await LoadAsync();
        Input = new() { Goal = Settings.TrainingGoal, Location = Settings.TrainingLocation, Revision = Settings.PreferencesRevision,
            Equipment = TrainingPreferences.Equipment.Where(e => Settings.AvailableEquipment.HasFlag(e.Value)).Select(e => e.Value).ToList(),
            Days = TrainingPreferences.SelectedDays(Settings.PreferredDays).ToList(), DaysLater = Settings.SetupStatus == SetupStatus.Completed && Settings.PreferredDays == 0 };
        return Page();
    }
    private async Task LoadAsync() => Settings = await db.DashboardSettings.AsNoTracking().SingleOrDefaultAsync(s => s.UserId == user.Id) ?? new() { UserId = user.Id };

    public async Task<IActionResult> OnPostAsync()
    {
        if (Request.Query.ContainsKey("ClientId") || Request.Form.ContainsKey("ClientId")) return NotFound();
        await LoadAsync();
        if (!ModelState.IsValid) return Page();
        var settings = await db.DashboardSettings.SingleOrDefaultAsync(s => s.UserId == user.Id);
        if ((settings?.PreferencesRevision ?? Guid.Empty) != Input.Revision) return PreferenceConflict();
        if (settings is null) { settings = new() { UserId = user.Id }; db.DashboardSettings.Add(settings); }
        settings.TrainingGoal = Input.Goal; settings.GoalTitle = TrainingPreferences.GoalTitle(Input.Goal);
        settings.TrainingLocation = Input.Location; settings.AvailableEquipment = Input.EquipmentMask; settings.PreferredDays = Input.DaysMask;
        if (!Input.DaysLater) settings.WeeklyWorkoutGoal = Input.Days.Distinct().Count();
        settings.SetupStatus = SetupStatus.Completed; settings.PreferencesRevision = Guid.NewGuid();
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return PreferenceConflict(); }
        catch (DbUpdateException) { ModelState.AddModelError("", "Не удалось сохранить настройки. Ваш выбор остался на странице; повторите сохранение."); return Page(); }
        return RedirectToPage(new { Saved = true, ReturnTo = ReturnTo == "profile" ? "profile" : null });
    }
    public async Task<IActionResult> OnPostSkipAsync()
    {
        if (Request.Query.ContainsKey("ClientId") || Request.Form.ContainsKey("ClientId")) return NotFound();
        ModelState.Clear();
        var settings = await db.DashboardSettings.SingleOrDefaultAsync(s => s.UserId == user.Id);
        if (settings?.SetupStatus == SetupStatus.Completed) return RedirectToPage(ReturnPage);
        if (settings is null) { settings = new() { UserId = user.Id }; db.DashboardSettings.Add(settings); }
        settings.SetupStatus = SetupStatus.Skipped; settings.PreferencesRevision = Guid.NewGuid();
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { await LoadAsync(); return PreferenceConflict(); }
        catch (DbUpdateException) { await LoadAsync(); ModelState.AddModelError("", "Не удалось отложить настройку. Повторите после восстановления соединения."); return Page(); }
        return RedirectToPage(ReturnPage);
    }
    private PageResult PreferenceConflict()
    {
        Conflict = true;
        ModelState.AddModelError("", "Настройки изменились в другой вкладке. Загрузите сохранённый вариант перед повторным изменением.");
        return Page();
    }
}
