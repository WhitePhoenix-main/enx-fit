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
public class ResendEmailConfirmationModel(UserManager<ApplicationUser> users, IEmailSender sender, ILogger<ResendEmailConfirmationModel> logger) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    public string ReturnUrl { get; private set; } = "/Dashboard";
    public bool CanSendEmail => AccountEmailDelivery.IsAvailable(sender);
    public string? StatusMessage { get; private set; }
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
        if (!CanSendEmail) ModelState.AddModelError(string.Empty, "Подтверждение email пока недоступно: отправка писем не подключена.");
        if (!ModelState.IsValid) return Page();
        var user = await users.FindByEmailAsync(Input.Email.Trim());
        if (user is not null && !user.EmailConfirmed)
        {
            var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(await users.GenerateEmailConfirmationTokenAsync(user)));
            var callback = Url.Page("/Account/ConfirmEmail", null, new { area = "Identity", userId = user.Id, code, returnUrl = ReturnUrl }, Request.Scheme)!;
            await AccountEmailDelivery.TrySendAsync(sender, logger, user.Email!, "Подтвердите email в Enix Fit",
                $"Подтвердите аккаунт: <a href='{HtmlEncoder.Default.Encode(callback)}'>подтвердить email</a>.", HttpContext.RequestAborted);
        }
        StatusMessage = "Если этот адрес связан с неподтверждённым аккаунтом, проверьте почту. Для подтверждения нужна ссылка из письма.";
        return Page();
    }
}
