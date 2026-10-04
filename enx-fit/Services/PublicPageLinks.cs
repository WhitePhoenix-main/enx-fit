using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace enx_fit.Services;

public static class PublicPageLinks
{
    public static string Open(IUrlHelper url, ClaimsPrincipal user, string page, object? values = null)
    {
        var destination = url.Page(page, new RouteValueDictionary(values) { ["area"] = "" }) ?? url.Content("~/Dashboard");
        return user.Identity?.IsAuthenticated == true ? destination :
            url.Page("/TechnicalPages/Login", "Register", new { area = "", returnUrl = destination })!;
    }
}
