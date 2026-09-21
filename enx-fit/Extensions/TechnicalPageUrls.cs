using enx_fit.Pages.TechnicalPages;
using Microsoft.AspNetCore.Mvc;

namespace enx_fit.Extensions;

public static class TechnicalPageUrls
{
    public static string SignInUrl(this IUrlHelper url)
        => url.PageUrl("/TechnicalPages/Login", nameof(LoginModel.OnGetLoginAsync), new { area = "" });

    public static string SignUpUrl(this IUrlHelper url)
        => url.PageUrl("/TechnicalPages/Login", nameof(LoginModel.OnGetRegisterAsync), new { area = "" });
}
