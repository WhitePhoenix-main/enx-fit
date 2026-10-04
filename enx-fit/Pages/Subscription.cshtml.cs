using enx_fit.Security;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;

namespace enx_fit.Pages;

[MinimumRole(UserRole.User)]
public sealed class SubscriptionModel(IFeatureAccessService features) : PageModel
{
    public FeatureAccess Access { get; private set; } = null!;
    public async Task<IActionResult> OnGetAsync() {
        if (Request.Query.ContainsKey("ClientId")) return NotFound();
        Access = await features.GetAsync(); Response.Headers.CacheControl = "no-store"; return Page();
    }
}
