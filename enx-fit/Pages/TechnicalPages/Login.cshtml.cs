using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using enx_fit.Areas.Identity.Data;
using enx_fit.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace enx_fit.Pages.TechnicalPages;

[AllowAnonymous]
public class LoginModel(
    SignInManager<ApplicationUser> signInManager,
    RegistrationService registration,
    IEmailSender emailSender,
    ILogger<LoginModel> logger) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    [TempData]
    public string? ErrorMessage { get; set; }

    public bool IsRegister { get; private set; }
    public string ReturnUrl { get; private set; } = "/";
    public IList<AuthenticationScheme> ExternalLogins { get; private set; } = [];
    public PasswordOptions PasswordRules => signInManager.UserManager.Options.Password;

    public class InputModel
    {
        [Required(ErrorMessage = "Введите email.")]
        [EmailAddress(ErrorMessage = "Введите корректный email.")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Введите пароль.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        // Confirmation is validated only by the registration handler.
        [DataType(DataType.Password)]
        public string? ConfirmPassword { get; set; }

        public bool RememberMe { get; set; } = true;
    }

    public Task OnGetAsync(string? returnUrl = null) => OnGetLoginAsync(returnUrl);

    public async Task OnGetLoginAsync(string? returnUrl = null)
    {
        await PrepareAsync(false, returnUrl);
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        if (!string.IsNullOrEmpty(ErrorMessage))
            ModelState.AddModelError(string.Empty, ErrorMessage);
    }

    public async Task OnGetRegisterAsync(string? returnUrl = null)
    {
        await PrepareAsync(true, returnUrl);
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
    }

    // Preserve old form submissions to the page without a named handler.
    public Task<IActionResult> OnPostAsync(string? returnUrl = null) => OnPostLoginAsync(returnUrl);

    public async Task<IActionResult> OnPostLoginAsync(string? returnUrl = null)
    {
        await PrepareAsync(false, returnUrl);
        if (!ModelState.IsValid) return Page();

        var account = await signInManager.UserManager.FindByEmailAsync(Input.Email.Trim());
        var result = account is null
            ? Microsoft.AspNetCore.Identity.SignInResult.Failed
            : await signInManager.PasswordSignInAsync(account, Input.Password, Input.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            logger.LogInformation("User logged in.");
            return LocalRedirect(ReturnUrl);
        }
        if (result.RequiresTwoFactor)
            return RedirectToPage("/Account/LoginWith2fa", new { area = "Identity", ReturnUrl, Input.RememberMe });
        if (result.IsLockedOut)
            return RedirectToPage("/Account/Lockout", new { area = "Identity" });

        ModelState.AddModelError(string.Empty, result.IsNotAllowed
            ? "Вход пока недоступен. Проверьте, подтверждён ли ваш email."
            : "Неверный email или пароль. Попробуйте ещё раз.");
        return Page();
    }

    public async Task<IActionResult> OnPostRegisterAsync(string? returnUrl = null)
    {
        await PrepareAsync(true, returnUrl);
        if (string.IsNullOrEmpty(Input.ConfirmPassword))
            ModelState.AddModelError("Input.ConfirmPassword", "Повторите пароль.");
        else if (Input.Password != Input.ConfirmPassword)
            ModelState.AddModelError("Input.ConfirmPassword", "Пароли не совпадают.");
        if (!ModelState.IsValid) return Page();

        var email = Input.Email.Trim();
        var user = new ApplicationUser(email) { Email = email };
        var result = await registration.RegisterAsync(user, Input.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, DescribeError(error));
            return Page();
        }

        logger.LogInformation("User created a new account with password.");
        var users = signInManager.UserManager;
        var code = await users.GenerateEmailConfirmationTokenAsync(user);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
        var callbackUrl = Url.Page("/Account/ConfirmEmail", null,
            new { area = "Identity", userId = user.Id, code, returnUrl = ReturnUrl }, Request.Scheme)!;
        await emailSender.SendEmailAsync(email, "Подтвердите email в Enix Fit",
            $"Подтвердите аккаунт: <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>подтвердить email</a>.");

        if (users.Options.SignIn.RequireConfirmedAccount)
            return RedirectToPage("/Account/RegisterConfirmation", new { area = "Identity", email, returnUrl = ReturnUrl });

        await signInManager.SignInAsync(user, isPersistent: false);
        return LocalRedirect(ReturnUrl);
    }

    public async Task<IActionResult> OnPostExternalLoginAsync(string provider, string? returnUrl = null)
    {
        await PrepareAsync(false, returnUrl);
        if (!ExternalLogins.Any(scheme => scheme.Name == provider))
        {
            ModelState.Clear();
            ModelState.AddModelError(string.Empty, "Этот способ входа пока недоступен. Используйте email и пароль.");
            return Page();
        }

        var callbackUrl = Url.Page("/Account/ExternalLogin", "Callback", new { area = "Identity", returnUrl = ReturnUrl });
        return Challenge(signInManager.ConfigureExternalAuthenticationProperties(provider, callbackUrl), provider);
    }

    private async Task PrepareAsync(bool register, string? returnUrl)
    {
        IsRegister = register;
        ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Content("~/Dashboard");
        ExternalLogins = (await signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
    }

    private string DescribeError(IdentityError error) => error.Code switch
    {
        "DuplicateUserName" or "DuplicateEmail" => "Аккаунт с таким email уже существует. Войдите или восстановите пароль.",
        "PasswordTooShort" => $"Пароль должен содержать не менее {PasswordRules.RequiredLength} символов.",
        "PasswordRequiresNonAlphanumeric" => "Добавьте в пароль специальный символ, например ! или @.",
        "PasswordRequiresDigit" => "Добавьте в пароль хотя бы одну цифру.",
        "PasswordRequiresLower" => "Добавьте в пароль строчную латинскую букву.",
        "PasswordRequiresUpper" => "Добавьте в пароль заглавную латинскую букву.",
        "PasswordRequiresUniqueChars" => $"Используйте не менее {PasswordRules.RequiredUniqueChars} разных символов.",
        "InvalidEmail" or "InvalidUserName" => "Проверьте правильность email.",
        _ => "Не удалось создать аккаунт. Проверьте данные и попробуйте ещё раз."
    };
}
