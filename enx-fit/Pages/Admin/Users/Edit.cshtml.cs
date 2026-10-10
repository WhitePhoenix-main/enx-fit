using System.ComponentModel.DataAnnotations;
using enx_fit.Areas.Identity.Data;
using enx_fit.Data;
using enx_fit.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace enx_fit.Pages.Admin.Users;
[MinimumRole(UserRole.Administrator)]
public class EditModel(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn, ApplicationDbContext db, CurrentUser currentUser) : PageModel
{
    [BindProperty(SupportsGet = true)] public string Id { get; set; } = "";
    [BindProperty] public UserEditInput Input { get; set; } = new();
    [BindProperty(SupportsGet = true)] public string? DirectorySearch { get; set; }
    [BindProperty(SupportsGet = true)] public UserRole? DirectoryRole { get; set; }
    [BindProperty(SupportsGet = true)] public int DirectoryPage { get; set; } = 1;
    public bool AccessExpanded => ModelState.Any(pair => pair.Key is "Input.Role" or "Input.TrainerId" or "Input.SubscriptionPlan" && pair.Value?.Errors.Count > 0);
    public bool IsCurrentUser => Id == currentUser.Id;
    public IEnumerable<UserRole> AvailableRoles => Enum.GetValues<UserRole>().Where(currentUser.HasMinimumRole);
    public int WorkoutCount { get; private set; }
    public int MeasurementCount { get; private set; }
    public IReadOnlyList<ApplicationUser> Trainers { get; private set; } = [];
    public async Task<IActionResult> OnGetAsync()
    {
        NormalizeDirectory();
        var user = await users.FindByIdAsync(Id);
        if (user is null) return NotFound();
        if (!currentUser.HasMinimumRole(user.Role)) return Forbid();
        Input = new() { UserName = user.UserName ?? "", Email = user.Email ?? "", PhoneNumber = user.PhoneNumber, Role = user.Role, TrainerId = user.TrainerId, SubscriptionPlan = user.SubscriptionPlan };
        await LoadCountsAsync();
        return Page();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        NormalizeDirectory();
        var user = await users.FindByIdAsync(Id);
        if (user is null) return NotFound();
        if (!currentUser.HasMinimumRole(user.Role)) return Forbid();
        Input.UserName = Input.UserName?.Trim() ?? "";
        Input.Email = Input.Email?.Trim() ?? "";
        if (IsCurrentUser && Input.Role != user.Role)
            ModelState.AddModelError("Input.Role", "Нельзя изменить роль своего аккаунта.");
        if (!currentUser.HasMinimumRole(Input.Role))
            ModelState.AddModelError("Input.Role", "Нельзя назначить роль выше собственной или несуществующий уровень.");
        var duplicate = await users.FindByEmailAsync(Input.Email);
        if (duplicate is not null && duplicate.Id != Id) ModelState.AddModelError("Input.Email", "Этот email уже используется другим пользователем.");
        if (!string.IsNullOrWhiteSpace(Input.TrainerId) && (Input.TrainerId == Id ||
            !await db.Users.AnyAsync(trainer => trainer.Id == Input.TrainerId && (trainer.Role == UserRole.Trainer || trainer.Role >= UserRole.Administrator))))
            ModelState.AddModelError("Input.TrainerId", "Выберите тренера из списка. Нельзя назначить пользователя самому себе.");
        if (!ModelState.IsValid) { await LoadCountsAsync(); return Page(); }
        await using var transaction = await db.Database.BeginTransactionAsync();
        if (!string.Equals(user.Email, Input.Email, StringComparison.OrdinalIgnoreCase)) user.EmailConfirmed = false;
        if (!string.Equals(user.PhoneNumber, Input.PhoneNumber?.Trim(), StringComparison.Ordinal)) user.PhoneNumberConfirmed = false;
        user.UserName = Input.UserName;
        user.Email = Input.Email;
        user.PhoneNumber = string.IsNullOrWhiteSpace(Input.PhoneNumber) ? null : Input.PhoneNumber.Trim();
        user.Role = Input.Role;
        if (Input.SubscriptionPlan.HasValue) user.SubscriptionPlan = Input.SubscriptionPlan.Value;
        user.TrainerId = string.IsNullOrWhiteSpace(Input.TrainerId) ? null : Input.TrainerId;
        var result = await users.UpdateAsync(user);
        if (result.Succeeded)
        {
            var wasAdministrator = await users.IsInRoleAsync(user, AppRoles.Administrator);
            var isAdministrator = user.Role >= UserRole.Administrator;
            if (wasAdministrator != isAdministrator)
                result = isAdministrator ? await users.AddToRoleAsync(user, AppRoles.Administrator) : await users.RemoveFromRoleAsync(user, AppRoles.Administrator);
        }
        if (result.Succeeded) result = await users.UpdateSecurityStampAsync(user);
        if (!result.Succeeded)
        {
            await transaction.RollbackAsync();
            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            await LoadCountsAsync();
            return Page();
        }
        await transaction.CommitAsync();
        if (IsCurrentUser) await signIn.RefreshSignInAsync(user);
        TempData["StatusMessage"] = "Данные пользователя сохранены.";
        return RedirectToPage(new { id = Id, DirectorySearch, DirectoryRole, DirectoryPage });
    }
    private async Task LoadCountsAsync()
    {
        WorkoutCount = await db.WorkoutSessions.CountAsync(w => w.UserId == Id);
        MeasurementCount = await db.BodyMeasurements.CountAsync(m => m.UserId == Id);
        Trainers = await db.Users.AsNoTracking().Where(user => (user.Role == UserRole.Trainer || user.Role >= UserRole.Administrator) && user.Id != Id)
            .OrderBy(user => user.UserName).ToListAsync();
    }
    private void NormalizeDirectory()
    {
        Response.Headers.CacheControl = "no-cache, no-store";
        DirectorySearch = DirectorySearch?.Trim(); if (DirectorySearch?.Length > 256) DirectorySearch = DirectorySearch[..256];
        if (DirectoryRole is { } role && !Enum.IsDefined(role)) DirectoryRole = null;
        DirectoryPage = Math.Clamp(DirectoryPage, 1, 1000000);
    }
}
public sealed class UserEditInput
{
    [EnumDataType(typeof(SubscriptionPlan))] public SubscriptionPlan? SubscriptionPlan { get; set; }
    [StringLength(450), Display(Name = "Персональный тренер")]
    public string? TrainerId { get; set; }
    [Required(ErrorMessage = "Укажите имя пользователя."), StringLength(256), Display(Name = "Имя пользователя")]
    public string UserName { get; set; } = "";
    [Required(ErrorMessage = "Укажите email."), EmailAddress(ErrorMessage = "Введите корректный email."), StringLength(256), Display(Name = "Email")]
    public string Email { get; set; } = "";
    [Phone(ErrorMessage = "Введите корректный номер телефона."), StringLength(32), Display(Name = "Телефон")]
    public string? PhoneNumber { get; set; }
    [EnumDataType(typeof(UserRole), ErrorMessage = "Выберите существующую роль."), Display(Name = "Роль")]
    public UserRole Role { get; set; } = UserRole.User;
}
