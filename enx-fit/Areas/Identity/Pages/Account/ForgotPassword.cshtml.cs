using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using enx_fit.Areas.Identity.Data;
using enx_fit.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace enx_fit.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class ForgotPasswordModel(UserManager<ApplicationUser> users, IEmailSender sender, ILogger<ForgotPasswordModel> logger) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    public string ReturnUrl { get; private set; } = "/Dashboard";
    public bool CanSendEmail => AccountEmailDelivery.IsAvailable(sender);
    public class InputModel
    {
        [Required(ErrorMessage = "Введите email.")]
        [EmailAddress(ErrorMessage = "Проверьте адрес электронной почты.")]
        public string Email { get; set; } = "";
    }
    public void OnGet(string? returnUrl = null) => ReturnUrl = AuthPageLinks.LocalReturnUrl(Url, returnUrl);
    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = AuthPageLinks.LocalReturnUrl(Url, returnUrl);
        if (!CanSendEmail) ModelState.AddModelError(string.Empty, "Восстановление по email пока недоступно: отправка писем не подключена.");
        if (!ModelState.IsValid) return Page();
        var user = await users.FindByEmailAsync(Input.Email.Trim());
        if (user is not null && await users.IsEmailConfirmedAsync(user))
        {
            var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(await users.GeneratePasswordResetTokenAsync(user)));
            var callback = Url.Page("/Account/ResetPassword", null, new { area = "Identity", code, returnUrl = ReturnUrl }, Request.Scheme)!;
            await AccountEmailDelivery.TrySendAsync(sender, logger, user.Email!, "Восстановление доступа в Enix Fit",
                $"Установите новый пароль: <a href='{HtmlEncoder.Default.Encode(callback)}'>изменить пароль</a>.", HttpContext.RequestAborted);
        }
        // The same neutral response is used for unknown and unconfirmed addresses and delivery failure.
        return RedirectToPage("./ForgotPasswordConfirmation", new { returnUrl = ReturnUrl });
    }
}
