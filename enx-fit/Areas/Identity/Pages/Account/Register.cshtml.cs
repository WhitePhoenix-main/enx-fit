using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Areas.Identity.Pages.Account;

// Compatibility endpoint; all account logic lives on the unified page.
[AllowAnonymous]
public class RegisterModel : PageModel
{
    public IActionResult OnGet(string? returnUrl = null)
        => RedirectToPage("/TechnicalPages/Login", "Register", new { area = "", returnUrl });

    public IActionResult OnPost(string? returnUrl = null)
        => RedirectToPagePreserveMethod("/TechnicalPages/Login", "Register", new { area = "", returnUrl });
}