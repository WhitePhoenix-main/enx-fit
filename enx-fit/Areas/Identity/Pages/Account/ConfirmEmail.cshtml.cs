using enx_fit.Areas.Identity.Data;
using enx_fit.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class ConfirmEmailModel(UserManager<ApplicationUser> users) : PageModel
{
    public string StatusMessage { get; private set; } = "";
    public bool Succeeded { get; private set; }
    public string ReturnUrl { get; private set; } = "/Dashboard";
    public async Task OnGetAsync(string? userId, string? code, string? returnUrl = null)
    {
        ReturnUrl = AuthPageLinks.LocalReturnUrl(Url, returnUrl);
        var user = userId is null ? null : await users.FindByIdAsync(userId);
        if (user is not null && AuthPageLinks.TryDecodeToken(code, out var token))
            Succeeded = (await users.ConfirmEmailAsync(user, token)).Succeeded;
        StatusMessage = Succeeded ? "Email подтверждён. Теперь можно войти в аккаунт." : "Ошибка: ссылка недействительна. Запросите новое письмо подтверждения.";
    }
}
