using enx_fit.Areas.Identity.Data;
using enx_fit.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class ConfirmEmailChangeModel(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn) : PageModel
{
    public string StatusMessage { get; private set; } = "Ошибка: ссылка недействительна. Запросите новое письмо в настройках email.";
    public bool Succeeded { get; private set; }
    public async Task OnGetAsync(string? userId, string? email, string? code)
    {
        var user = userId is null ? null : await users.FindByIdAsync(userId);
        if (user is null || string.IsNullOrWhiteSpace(email) || !AuthPageLinks.TryDecodeToken(code, out var token)) return;
        if (!(await users.ChangeEmailAsync(user, email, token)).Succeeded) return;
        if (!(await users.SetUserNameAsync(user, email)).Succeeded)
        { StatusMessage = "Ошибка: email подтверждён, но не удалось обновить логин. Обратитесь в поддержку."; return; }
        // Opening another account's mail link must not replace the current login session.
        if (users.GetUserId(User) == user.Id) await signIn.RefreshSignInAsync(user);
        Succeeded = true;
        StatusMessage = "Email изменён и подтверждён. Для входа используйте новый адрес.";
    }
}
