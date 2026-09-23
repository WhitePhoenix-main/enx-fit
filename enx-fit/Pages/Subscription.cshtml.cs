using enx_fit.Security;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Pages;

[MinimumRole(UserRole.User)]
public sealed class SubscriptionModel(IFeatureAccessService features) : PageModel
{
    public FeatureAccess Access { get; private set; } = null!;
    public async Task OnGetAsync() { Access = await features.GetAsync(); Response.Headers.CacheControl = "no-store"; }
}
