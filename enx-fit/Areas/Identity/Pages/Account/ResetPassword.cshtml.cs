using System.ComponentModel.DataAnnotations;
using enx_fit.Areas.Identity.Data;
using enx_fit.Areas.Identity.Pages.Account.Manage;
using enx_fit.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class ResetPasswordModel(UserManager<ApplicationUser> users) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    public string ReturnUrl { get; private set; } = "/Dashboard";
    public bool HasValidLink { get; private set; }
    public string PasswordHint => AccountMessages.PasswordHint(users.Options.Password);
    public class InputModel
    {
        [Required(ErrorMessage = "Введите email.")]
        [EmailAddress(ErrorMessage = "Проверьте адрес электронной почты.")]
        public string Email { get; set; } = "";
        [Required(ErrorMessage = "Введите новый пароль.")]
        [StringLength(100, ErrorMessage = "Пароль должен содержать от {2} до {1} символов.", MinimumLength = 6)]
        [DataType(DataType.Password)] public string Password { get; set; } = "";
        [Required(ErrorMessage = "Повторите новый пароль.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Пароли не совпадают.")]
        public string ConfirmPassword { get; set; } = "";
        [Required(ErrorMessage = "Запросите новую ссылку восстановления.")]
        public string Code { get; set; } = "";
    }
    public void OnGet(string? code = null, string? returnUrl = null)
    {
        ReturnUrl = AuthPageLinks.LocalReturnUrl(Url, returnUrl);
        HasValidLink = AuthPageLinks.TryDecodeToken(code, out var token);
        Input.Code = token;
    }
    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = AuthPageLinks.LocalReturnUrl(Url, returnUrl);
        HasValidLink = !string.IsNullOrWhiteSpace(Input.Code);
        if (!ModelState.IsValid) return Page();
        var user = await users.FindByEmailAsync(Input.Email.Trim());
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Не удалось изменить пароль. Проверьте email и запросите новую ссылку.");
            return Page();
        }
        var result = await users.ResetPasswordAsync(user, Input.Code, Input.Password);
        if (result.Succeeded) {
            TempData["AccountPasswordResetComplete"] = true;
            return RedirectToPage("./ResetPasswordConfirmation", new { returnUrl = ReturnUrl });
        }
        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Code == "InvalidToken"
                ? "Ссылка недействительна или уже использована. Запросите новую."
                : AccountMessages.PasswordError(error, users.Options.Password));
        return Page();
    }
}
