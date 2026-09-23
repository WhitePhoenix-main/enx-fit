using enx_fit.Models;
using enx_fit.Security;
using enx_fit.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Pages.Trainer;

[MinimumRole(UserRole.Trainer)]
public sealed class ClientModel(DashboardService dashboard, TrainingProgramService programs,
    IFeatureAccessService features, CoachAttentionService attention) : PageModel
{
    public DashboardData Data { get; private set; } = null!;
    public List<TrainingProgram> Programs { get; private set; } = [];
    public CoachAttention? Attention { get; private set; }
    public async Task<IActionResult> OnGetAsync(string id)
    {
        if (!await features.CanUseAsync(Feature.CoachClientManagement)) return Forbid();
        var subject = await dashboard.GetSubjectAsync(id);
        if (subject is null) return NotFound();
        Data = await dashboard.LoadAsync(subject, 30, DateOnly.FromDateTime(DateTime.UtcNow));
        Programs = (await programs.ListAsync("assigned")).Where(p => p.OwnerId == id).ToList();
        if (await features.CanUseAsync(Feature.CoachClientAlerts)) Attention = await attention.LoadAsync(id);
        Response.Headers.CacheControl = "no-store";
        return Page();
    }
}
