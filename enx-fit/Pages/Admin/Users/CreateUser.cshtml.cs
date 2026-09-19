using System.ComponentModel.DataAnnotations;
using enx_fit.Areas.Identity.Data;
using enx_fit.Data;
using enx_fit.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

// ReSharper disable once CheckNamespace
namespace enx_fit.Admin;

[MinimumRole(UserRole.Administrator)]
public class CreateUserModel(UserManager<ApplicationUser> users, ApplicationDbContext db) : PageModel
{
    [BindProperty] public NewUserInput Input { get; set; } = new();
    public void OnGet() { }
    public async Task<IActionResult> OnPostAsync()
    {
        Input.Email = Input.Email?.Trim() ?? "";
        if (await users.FindByEmailAsync(Input.Email) is not null) ModelState.AddModelError("Input.Email", "Пользователь с таким email уже существует.");
        if (!ModelState.IsValid) return Page();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var user = new ApplicationUser { UserName = Input.Email, Email = Input.Email };
        var result = await users.CreateAsync(user, Input.Password);
        if (result.Succeeded) result = await users.AddToRoleAsync(user, AppRoles.User);
        if (!result.Succeeded)
        {
            await transaction.RollbackAsync();
            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            return Page();
        }
        await transaction.CommitAsync();
        TempData["StatusMessage"] = "Пользователь создан. При необходимости измените профиль и права доступа.";
        return RedirectToPage("./Edit", new { id = user.Id });
    }
}
public sealed class NewUserInput
{
    [Required(ErrorMessage = "Укажите email."), EmailAddress(ErrorMessage = "Введите корректный email."), StringLength(256), Display(Name = "Email")]
    public string Email { get; set; } = "";
    [Required(ErrorMessage = "Укажите пароль."), StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль должен содержать от 6 до 100 символов."), DataType(DataType.Password), Display(Name = "Пароль")]
    public string Password { get; set; } = "";
    [Required(ErrorMessage = "Повторите пароль."), Compare(nameof(Password), ErrorMessage = "Пароли не совпадают."), DataType(DataType.Password), Display(Name = "Подтверждение пароля")]
    public string ConfirmPassword { get; set; } = "";
}
