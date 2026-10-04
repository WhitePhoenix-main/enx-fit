using System.Security.Claims;
using enx_fit.Areas.Identity.Data;
using enx_fit.Pages;
using enx_fit.Security;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;

namespace enx_fit.ViewModels;

public sealed record MobileAppNavigationViewModel(
    string Page, DashboardModel? Dashboard, bool Reference, bool IsTrainer, bool IsAdministrator)
{
    public RouteValueDictionary RouteValues => new(Dashboard?.NavigationValues ?? (Reference ? new { Reference = true } : null))
    {
        ["area"] = ""
    };

    public static MobileAppNavigationViewModel Create(ViewDataDictionary viewData, ClaimsPrincipal user, string? page)
    {
        var dashboard = viewData["Dashboard"] as DashboardModel;
        var reference = dashboard?.Reference == true || viewData["Reference"] is true;
        var administrator = user.HasMinimumRole(UserRole.Administrator);
        return new(page ?? "", dashboard, reference,
            !reference && (user.GetUserRole() == UserRole.Trainer || administrator), !reference && administrator);
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
