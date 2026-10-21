using System.Security.Claims;
using enx_fit.Pages.Dashboard;
using enx_fit.Security;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace enx_fit.Presentation.Navigation;

public sealed record BrandPresentation(string Theme, string Color, string IconSuffix)
{
    public static async Task<BrandPresentation> ResolveAsync(ViewDataDictionary viewData, ClaimsPrincipal user, IFeatureAccessService access)
    {
        var preview = (viewData["Dashboard"] as DashboardModel)?.SubscriptionHome;
        var plan = preview?.Preview == true && Enum.TryParse<SubscriptionPlan>(preview.Badge, out var referencePlan)
            ? referencePlan
            : user.Identity?.IsAuthenticated == true ? (await access.GetAsync()).Plan : SubscriptionPlan.Free;
        return plan switch
        {
            SubscriptionPlan.Pro => new("pro", "#ffad52", "-pro"),
            SubscriptionPlan.Coach => new("coach", "#ff4668", "-coach"),
            _ => new("free", "#4389ff", "")
        };
    }
}
