using enx_fit.Models;
using enx_fit.Security;
using enx_fit.Services;
using enx_fit.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace enx_fit.Pages.Programs;

public sealed class AssignModel(TrainingProgramService programs) : ProgramPageModel(programs)
{
    public TrainingProgram Item { get; private set; } = null!;
    public List<ProgramClient> Clients { get; private set; } = [];
    [BindProperty] public AssignmentInput Input { get; set; } = new();
    private async Task<bool> LoadAsync(int id)
    {
        await LoadAccessAsync();
        if (!Access.CanUse(Feature.CoachProgramAssignment)) { Error(new(ProgramFailure.Forbidden)); return false; }
        var p = await Programs.FindAsync(id);
        if (p is null || p.IsArchived) { Error(new(ProgramFailure.NotFound)); return false; }
        Item = p; Clients = await Programs.ClientsAsync(); return true;
    }
    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (await LoadAsync(id)) Input.Days = Item.Workouts.OrderBy(w => w.Order).Select(w => w.DayOfWeek ?? 0).ToList();
        return Page();
    }
    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!await LoadAsync(id) || !ModelState.IsValid) return Page();
        try
        {
            var copy = await Programs.AssignAsync(id, Input);
            TempData["StatusMessage"] = "Клиенту назначена отдельная копия. Её нагрузку можно изменить независимо от исходной программы.";
            return RedirectToPage("Details", new { id = copy });
        }
        catch (ProgramOperationException e) { return Error(e); }
        catch (DbUpdateException e) { SaveError(e); return Page(); }
    }
}
