using enx_fit.Security;
using enx_fit.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Pages.Trainer;

[MinimumRole(UserRole.Trainer)]
public sealed class ClientsModel(DashboardService dashboard, IFeatureAccessService features, CoachAttentionService attention) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    public IReadOnlyList<ClientSummary> Clients { get; private set; } = [];
    public CoachAttention? Attention { get; private set; }
    public bool CanManage { get; private set; }
    public async Task OnGetAsync()
    {
        Clients = await dashboard.GetClientsAsync(Search);
        CanManage = await features.CanUseAsync(Feature.CoachClientManagement);
        if (await features.CanUseAsync(Feature.CoachClientAlerts)) Attention = await attention.LoadAsync();
        Response.Headers.CacheControl = "no-cache, no-store";
    }
}
