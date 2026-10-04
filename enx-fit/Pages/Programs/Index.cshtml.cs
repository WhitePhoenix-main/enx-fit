using enx_fit.Models;
using enx_fit.Services;
using enx_fit.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using enx_fit.Data;

namespace enx_fit.Pages.Programs;

public sealed class IndexModel(TrainingProgramService programs, CurrentUser currentUser, ApplicationDbContext db) : ProgramPageModel(programs)
{
    [BindProperty(SupportsGet = true)] public string? Tab { get; set; } = "mine";
    [BindProperty(SupportsGet = true)] public string? Category { get; set; }
    [BindProperty(SupportsGet = true)] public bool Archived { get; set; }
    [BindProperty(SupportsGet = true)] public bool Reference { get; set; }
    [BindProperty(SupportsGet = true)] public int WeekOffset { get; set; }
    [BindProperty(SupportsGet = true)] public ProgramLevel? Level { get; set; }
    [BindProperty(SupportsGet = true)] public int? Days { get; set; }
    [BindProperty(SupportsGet = true)] public bool All { get; set; }
    [BindProperty(SupportsGet = true)] public bool MatchPreferences { get; set; }
    public DashboardSettings? Preferences { get; private set; }
    public bool HasPreferences => Preferences?.SetupStatus == SetupStatus.Completed;
    public string ViewerId => currentUser.Id;
    public int OwnCount { get; private set; }
    public List<TrainingProgram> Items { get; private set; } = [];
    public Dictionary<int, List<WorkoutSession>> Sessions { get; } = [];
    public static readonly string[] Categories = ["Для начинающих", "Набор массы", "Сила", "Снижение веса", "Upper / Lower", "Push / Pull / Legs", "Full Body"];
    public async Task<IActionResult> OnGetAsync(bool upgrade = false)
    {
        await LoadAsync(); ShowUpgrade = upgrade;
        return Page();
    }
    private async Task LoadAsync()
    {
        await LoadAccessAsync();
        ViewData["Reference"] = Reference;
        WeekOffset = Math.Clamp(WeekOffset, -52, 52);
        if (Level is { } level && !Enum.IsDefined(level)) Level = null;
        if (Days is < 1 or > 7) Days = null;
        if (Tab is not ("mine" or "templates" or "assigned")) Tab = "mine";
        if (Tab == "templates" && !Reference) {
            Preferences = await db.DashboardSettings.AsNoTracking().SingleOrDefaultAsync(s => s.UserId == currentUser.Id);
            if (MatchPreferences && HasPreferences) {
                if (!Request.Query.ContainsKey("Level")) Level = ProgramLevel.Beginner;
                if (!Request.Query.ContainsKey("Days")) { var count = TrainingPreferences.SelectedDays(Preferences!.PreferredDays).Length; if (count > 0) Days = count; }
            }
        }
        OwnCount = await Programs.OwnCountAsync();
        if (Tab == "assigned" && !Access.CanUse(Feature.CoachProgramAssignment))
        {
            if (!Access.IsCoach) Error(new(ProgramFailure.Forbidden));
            return;
        }
        Items = await Programs.ListAsync(Tab, Archived, Category);
        if (Tab == "templates" && Level is { } filter) Items = Items.Where(p => p.Level == filter).ToList();
        if (Tab == "templates" && Days is { } days) Items = Items.Where(p => p.DaysPerWeek == days).ToList();
        if (Tab == "templates" && MatchPreferences && HasPreferences) Items = Items.Where(p => TrainingPreferences.HasEquipment(p, Preferences!.AvailableEquipment)).ToList();
        if (Tab == "templates") Items = Items.OrderBy(p => p.Level).ThenBy(p => p.DaysPerWeek).ThenBy(p => p.Name).ToList();
        foreach (var p in Items.Where(p => p.StartDate.HasValue)) Sessions[p.Id] = await Programs.SessionsAsync(p);
    }
    public async Task<IActionResult> OnPostDuplicateAsync(int id)
    {
        await LoadAccessAsync();
        try { return RedirectToPage("Details", new { id = await Programs.DuplicateAsync(id) }); }
        catch (ProgramOperationException e) { await LoadAsync(); return Error(e); }
        catch (DbUpdateException e) { SaveError(e); await LoadAsync(); return Page(); }
    }
    public async Task<IActionResult> OnPostArchiveAsync(int id, bool restore)
    {
        await LoadAccessAsync();
        try { await Programs.ArchiveAsync(id, restore); TempData["StatusMessage"] = restore ? "Программа восстановлена." : "Программа перемещена в архив."; return RedirectToPage(new { Tab, Archived }); }
        catch (ProgramOperationException e) { await LoadAsync(); return Error(e); }
        catch (DbUpdateException e) { SaveError(e); await LoadAsync(); return Page(); }
    }
    public async Task<IActionResult> OnPostStartAsync(int id)
    {
        await LoadAccessAsync();
        try { return RedirectToPage("/Workouts/Details", new { id = await Programs.StartNextAsync(id) }); }
        catch (ProgramOperationException e) { await LoadAsync(); return Error(e); }
        catch (DbUpdateException e) { SaveError(e); await LoadAsync(); return Page(); }
    }
}
