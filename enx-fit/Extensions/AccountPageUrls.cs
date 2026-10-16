using enx_fit.Pages.Account;
using Microsoft.AspNetCore.Mvc;

namespace enx_fit.Extensions;

public static class AccountPageUrls
{
    public static string SignInUrl(this IUrlHelper url)
        => url.PageUrl("/Account/Login", nameof(LoginModel.OnGetLoginAsync), new { area = "" });

    public static string SignUpUrl(this IUrlHelper url)
        => url.PageUrl("/Account/Login", nameof(LoginModel.OnGetRegisterAsync), new { area = "" });
}
