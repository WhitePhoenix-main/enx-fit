using enx_fit.Models;
using enx_fit.Security;
using enx_fit.Services;
using enx_fit.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Pages.Trainer;

[MinimumRole(UserRole.Trainer)]
public sealed class ClientModel(DashboardService dashboard, TrainingProgramService programs,
    IFeatureAccessService features, CoachAttentionService attention, CurrentUser user) : PageModel
{
    public DashboardData Data { get; private set; } = null!;
    public List<TrainingProgram> Programs { get; private set; } = [];
    public List<TrainingProgram> ArchivedPrograms { get; private set; } = [];
    public List<TrainingProgram> Sources { get; private set; } = [];
    public IReadOnlyList<ClientSummary> Clients { get; private set; } = [];
    public CoachAttention? Attention { get; private set; }
    public bool CanAssign { get; private set; }
    public DateOnly Today => user.LocalToday;
    [BindProperty(SupportsGet = true)] public string? DirectorySearch { get; set; }
    [BindProperty(SupportsGet = true)] public string? DirectoryScope { get; set; }
    [BindProperty(SupportsGet = true)] public string? DirectorySort { get; set; }
    public async Task<IActionResult> OnGetAsync(string id)
    {
        if (!await features.CanUseAsync(Feature.CoachClientManagement)) return Forbid();
        var subject = await dashboard.GetSubjectAsync(id);
        if (subject is null || subject.Id == user.Id) return NotFound();
        Data = await dashboard.LoadAsync(subject, 30, Today);
        Clients = await dashboard.GetClientsAsync();
        ViewData["CoachContext"] = new CoachContextViewModel(subject.Id, Data.Name, Clients);
        CanAssign = await features.CanUseAsync(Feature.CoachProgramAssignment);
        if (CanAssign)
        {
            Programs = (await programs.ListAsync("assigned")).Where(p => p.OwnerId == id).ToList();
            ArchivedPrograms = (await programs.ListAsync("assigned", archived: true)).Where(p => p.OwnerId == id).ToList();
            Sources = (await programs.ListAsync("mine")).Where(p => p.Assignment is null)
                .Concat(await programs.ListAsync("templates"))
                .Where(p => p.Workouts.Count > 0 && p.Workouts.All(w => w.Exercises.Count > 0))
                .OrderBy(p => p.IsTemplate).ThenBy(p => p.Name).ToList();
        }
        if (await features.CanUseAsync(Feature.CoachClientAlerts)) Attention = await attention.LoadAsync(id);
        Response.Headers.CacheControl = "no-cache, no-store";
        DirectorySearch = DirectorySearch?.Trim();
        if (DirectorySearch?.Length > 100) DirectorySearch = DirectorySearch[..100];
        DirectoryScope = DirectoryScope is "attention" or "empty" ? DirectoryScope : "all";
        DirectorySort = DirectorySort is "name" or "recent" ? DirectorySort : "attention";
        return Page();
    }
    public async Task<IActionResult> OnGetSelectProgramAsync(string id, int sourceId)
    {
        if (!await features.CanUseAsync(Feature.CoachProgramAssignment)) return Forbid();
        var subject = await dashboard.GetSubjectAsync(id);
        if (subject is null || subject.Id == user.Id) return NotFound();
        var source = await programs.FindAsync(sourceId);
        if (source is null || source.IsArchived || (!source.IsTemplate && (source.OwnerId != user.Id || source.Assignment is not null))) return NotFound();
        return RedirectToPage("/Programs/Assign", new { id = sourceId, ClientId = id });
    }
}
