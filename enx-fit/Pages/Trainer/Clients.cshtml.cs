using enx_fit.Security;
using enx_fit.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Pages.Trainer;

[MinimumRole(UserRole.Trainer)]
public sealed class ClientsModel(DashboardService dashboard, IFeatureAccessService features, CoachAttentionService attention, CurrentUser user) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    [BindProperty(SupportsGet = true)] public string Scope { get; set; } = "all";
    [BindProperty(SupportsGet = true)] public string Sort { get; set; } = "attention";
    [BindProperty(SupportsGet = true)] public string? OpenClientId { get; set; }
    public IReadOnlyList<ClientSummary> Clients { get; private set; } = [];
    public CoachAttention? Attention { get; private set; }
    public bool CanManage { get; private set; }
    public bool CanAlerts { get; private set; }
    public int Total { get; private set; }
    public int Found { get; private set; }
    public IReadOnlySet<string> AttentionIds { get; private set; } = new HashSet<string>();
    public DateOnly Today => user.LocalToday;
    public async Task<IActionResult> OnGetAsync()
    {
        Response.Headers.CacheControl = "no-cache, no-store";
        var access = await features.GetAsync();
        if (!access.IsCoach) return Forbid();
        var all = await dashboard.GetClientsAsync();
        Total = all.Count;
        CanManage = access.CanUse(Feature.CoachClientManagement);
        CanAlerts = access.CanUse(Feature.CoachClientAlerts);
        if (!string.IsNullOrWhiteSpace(OpenClientId))
        {
            if (all.All(c => c.Id != OpenClientId)) return NotFound();
            return CanManage ? RedirectToPage("/Trainer/Client", new { id = OpenClientId })
                : RedirectToPage("/Dashboard", new { ClientId = OpenClientId });
        }
        Search = Search?.Trim();
        if (Search?.Length > 100) Search = Search[..100];
        if (Scope is not ("all" or "attention" or "empty") || (Scope == "attention" && !CanAlerts)) Scope = "all";
        if (Sort is not ("attention" or "name" or "recent")) Sort = "attention";
        var found = await dashboard.GetClientsAsync(Search);
        Found = found.Count;
        if (CanAlerts)
        {
            Attention = await attention.LoadAsync(search: Search, assignedOnly: true);
            AttentionIds = Attention.Alerts.Select(a => a.ClientId).ToHashSet();
        }
        var visible = found.Where(c => Scope == "all" || (Scope == "attention" ? AttentionIds.Contains(c.Id) : c.LastWorkout is null));
        Clients = (Sort == "name" ? visible.OrderBy(c => c.Name, StringComparer.CurrentCultureIgnoreCase)
            : Sort == "recent" ? visible.OrderByDescending(c => c.LastWorkout).ThenBy(c => c.Name, StringComparer.CurrentCultureIgnoreCase)
            : visible.OrderByDescending(c => AttentionIds.Contains(c.Id)).ThenBy(c => c.Name, StringComparer.CurrentCultureIgnoreCase)).ToList();
        ModelState.Clear();
        return Page();
    }
}
