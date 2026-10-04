using System.Security.Claims;
using enx_fit.Areas.Identity.Data;
using enx_fit.Pages;
using enx_fit.Security;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;

namespace enx_fit.ViewModels;

public sealed record MobileAppNavigationViewModel(
    string Page, DashboardModel? Dashboard, bool Reference, bool IsTrainer, bool IsAdministrator, CoachContextViewModel? Client = null)
{
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
        if (page == "/Programs/Index" && Client is not null)
            return Client.CanManage ? url.Page("/Trainer/Client", new { area = "", id = Client.Id }) + "#client-programs"
                : url.Page("/Dashboard/Plans", RouteValues);
        var personal = page is "/Profile" or "/Subscription" or "/Trainer/Clients" or "/Exercises/Index" || page.StartsWith("/Admin/");
        return url.Page(page, personal ? new RouteValueDictionary { ["area"] = "", ["Reference"] = Reference ? true : null } : RouteValues);
    }

    public bool IsActive(string section) => section switch
    {
        "home" => Page.Equals("/Dashboard", StringComparison.OrdinalIgnoreCase),
        "workouts" => Page.Equals("/Dashboard/Workouts", StringComparison.OrdinalIgnoreCase) || In("/Workouts"),
        "programs" => Page.Equals("/Dashboard/Plans", StringComparison.OrdinalIgnoreCase) || In("/Programs"),
        "progress" => Page is "/Dashboard/Progress" or "/Dashboard/Analytics" or "/Dashboard/Achievements" || In("/Body") || In("/Analytics"),
        "more" => !IsActive("home") && !IsActive("workouts") && !IsActive("programs") && !IsActive("progress"),
        _ => false
    };

    private bool In(string root) => Page.Equals(root, StringComparison.OrdinalIgnoreCase)
        || Page.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase);
}
