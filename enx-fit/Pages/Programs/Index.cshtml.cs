using enx_fit.Models;
using enx_fit.Services;
using enx_fit.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace enx_fit.Pages.Programs;

public sealed class IndexModel(TrainingProgramService programs) : ProgramPageModel(programs)
{
    [BindProperty(SupportsGet = true)] public string? Tab { get; set; } = "mine";
    [BindProperty(SupportsGet = true)] public string? Category { get; set; }
    [BindProperty(SupportsGet = true)] public bool Archived { get; set; }
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
        if (Tab is not ("mine" or "templates" or "assigned")) Tab = "mine";
        OwnCount = await Programs.OwnCountAsync();
        if (Tab == "assigned" && !Access.CanUse(Feature.CoachProgramAssignment))
        {
            if (!Access.IsCoach) Error(new(ProgramFailure.Forbidden));
            return;
        }
        Items = await Programs.ListAsync(Tab, Archived, Category);
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
}
