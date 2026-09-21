using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class LoginModel : PageModel
{
    public IActionResult OnGet(string? returnUrl = null)
        => RedirectToPage("/TechnicalPages/Login", "Login", new { area = "", returnUrl });

    public IActionResult OnPost(string? returnUrl = null)
        => RedirectToPagePreserveMethod("/TechnicalPages/Login", "Login", new { area = "", returnUrl });
}