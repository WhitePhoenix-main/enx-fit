using enx_fit.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class RegisterConfirmationModel(IEmailSender sender) : PageModel
{
    public string Email { get; private set; } = "";
    public string ReturnUrl { get; private set; } = "/Dashboard";
    public bool CanSendEmail => AccountEmailDelivery.IsAvailable(sender);
    public bool EmailSent { get; private set; }
    public IActionResult OnGet(string? email, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(email)) return RedirectToPage("/TechnicalPages/Login", new { area = "", handler = "Register" });
        Email = email;
        ReturnUrl = AuthPageLinks.LocalReturnUrl(Url, returnUrl);
        var sent = TempData["AccountConfirmationEmailSent"] is true;
        var recipient = TempData["AccountConfirmationEmailRecipient"] as string;
        EmailSent = sent && string.Equals(recipient, email, StringComparison.OrdinalIgnoreCase);
        // This public page must never generate or expose a confirmation token.
        return Page();
    }
}
