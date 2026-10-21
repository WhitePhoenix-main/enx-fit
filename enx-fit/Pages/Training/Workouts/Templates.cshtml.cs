using enx_fit.Models;
using enx_fit.Security;
using enx_fit.Services.Programs;
using enx_fit.Services.Workouts;
using enx_fit.ViewModels.Workouts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Pages.Training.Workouts;

[MinimumRole(UserRole.User)]
public sealed class TemplatesModel(WorkoutTemplateCatalog catalog, WorkoutService workouts, CurrentUser user, IFeatureAccessService features) : PageModel
{
    public FeatureAccess Access { get; private set; } = null!;
    public IReadOnlyList<WorkoutTemplateCollection> Collections { get; private set; } = [];
    public IReadOnlyList<WorkoutTemplateCard> Cards { get; private set; } = [];
    [BindProperty(SupportsGet=true)] public int? Selected { get; set; }
    [BindProperty(SupportsGet=true)] public string? Category { get; set; }
    public string ViewerId => user.Id;
    public int? ActiveId { get; private set; }
    public async Task OnGetAsync()
    {
        Cards=await catalog.ListAsync(); ActiveId=(await workouts.ActiveAsync())?.Id;
        Access = await features.GetAsync();
        if (Access.Plan != SubscriptionPlan.Free) Collections = await catalog.CollectionsAsync();
        if (!Cards.Any(c => c.Workout.Id == Selected)) Selected=Cards.FirstOrDefault()?.Workout.Id;
        Response.Headers.CacheControl="no-cache, no-store";
    }
    public async Task<IActionResult> OnPostStartAsync(int templateId)
    {
        try { return RedirectToPage("./Details",new { id=await workouts.StartAsync("template",templateId:templateId) }); }
        catch (InvalidOperationException ex) { ModelState.AddModelError("",ex.Message); await OnGetAsync(); return Page(); }
    }
    public async Task<IActionResult> OnPostCollectionAsync(string name, List<int> templateIds)
    {
        try { await catalog.SaveCollectionAsync(name, templateIds); TempData["StatusMessage"] = "Коллекция сохранена."; return RedirectToPage(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (ArgumentException error) { ModelState.AddModelError("", error.Message); await OnGetAsync(); return Page(); }
    }
    public async Task<IActionResult> OnPostDeleteCollectionAsync(int id) => await catalog.DeleteCollectionAsync(id) ? RedirectToPage() : NotFound();
    public async Task<IActionResult> OnPostCopyAsync(int templateId,bool edit=false)
    {
        try {
            var card=(await catalog.ListAsync()).SingleOrDefault(c => c.Workout.Id == templateId);
            if (card is null) return NotFound();
            var id=edit && card.Mine ? card.Program.Id : await catalog.CopyAsync(templateId);
            if (edit) return RedirectToPage("/Training/Programs/Edit",new { id });
            TempData["StatusMessage"]="Копия добавлена в ваши шаблоны.";
            return RedirectToPage(new { Selected=(await catalog.ListAsync()).First(c => c.Program.Id==id).Workout.Id });
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (ProgramOperationException ex) { ModelState.AddModelError("",ex.Message); await OnGetAsync(); return Page(); }
    }
}
