using enx_fit.Pages.Dashboard;
using System.Security.Claims;
using enx_fit.Areas.Identity.Data;
using enx_fit.Security;
using enx_fit.ViewModels.Coaching;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;

namespace enx_fit.ViewModels.Navigation;

public sealed record MobileAppNavigationViewModel(
    string Page, DashboardModel? Dashboard, bool Reference, bool IsTrainer, bool IsAdministrator, CoachContextViewModel? Client = null)
{
    public bool IsAdminShell { get; init; }

    public RouteValueDictionary RouteValues => new(Dashboard?.NavigationValues ?? (Client is not null ? new { ClientId = Client.Id } : Reference ? new { Reference = true } : null))
    {
        ["area"] = ""
    };

    public static MobileAppNavigationViewModel Create(ViewDataDictionary viewData, ClaimsPrincipal user, string? page)
    {
        var dashboard = viewData["Dashboard"] as DashboardModel;
        var reference = dashboard?.Reference == true || viewData["Reference"] is true;
        var administrator = user.HasMinimumRole(UserRole.Administrator);
        return new(page ?? "", dashboard, reference,
            !reference && (user.GetUserRole() == UserRole.Trainer || administrator), !reference && administrator,
            reference ? null : CoachContextViewModel.Resolve(viewData));
    }

    public string? PageUrl(IUrlHelper url, string page)
    {
        if (page == "/Training/Programs/Index" && Client is not null)
            return Client.CanManage ? url.Page("/Coaching/Client", new { area = "", id = Client.Id }) + "#client-programs"
                : url.Page("/Training/Plans", RouteValues);
        var personal = page is "/Account/Profile" or "/Account/Subscription" or "/Coaching/Clients" or "/Training/Exercises/Index" || page.StartsWith("/Admin/");
        return url.Page(page, personal ? new RouteValueDictionary { ["area"] = "", ["Reference"] = Reference ? true : null } : RouteValues);
    }

    public bool IsActive(string section) => section switch
    {
        "home" => Page.Equals("/Dashboard/Index", StringComparison.OrdinalIgnoreCase),
        "workouts" => Page.Equals("/Training/Index", StringComparison.OrdinalIgnoreCase) || Page.Equals("/Training/Records", StringComparison.OrdinalIgnoreCase) || In("/Training/Workouts"),
        "programs" => Page.Equals("/Training/Plans", StringComparison.OrdinalIgnoreCase) || In("/Training/Programs"),
        "progress" => In("/Progress"),
        "more" => !IsActive("home") && !IsActive("workouts") && !IsActive("programs") && !IsActive("progress"),
        _ => false
    };

    private bool In(string root) => Page.Equals(root, StringComparison.OrdinalIgnoreCase)
        || Page.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase);
}
