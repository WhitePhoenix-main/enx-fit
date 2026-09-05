using enx_fit.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace enx_fit.Pages.Admin.Users;

public class IndexModel(
    UserManager<IdentityUser> userManager,
    CurrentUser currentUser) : PageModel
{
    public IReadOnlyList<UserRoleRow> Users { get; private set; } = [];

    public async Task OnGetAsync() => await LoadUsersAsync();

    public async Task<IActionResult> OnPostSetAdministratorAsync(string userId, bool enabled)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return NotFound();
        }

        if (!enabled && userId == currentUser.Id)
        {
            TempData["ErrorMessage"] = "Нельзя снять роль администратора у текущего аккаунта.";
            return RedirectToPage();
        }

        var isAdministrator = await userManager.IsInRoleAsync(user, AppRoles.Administrator);
        IdentityResult result = IdentityResult.Success;

        if (enabled && !isAdministrator)
        {
            result = await userManager.AddToRoleAsync(user, AppRoles.Administrator);
        }
        else if (!enabled && isAdministrator)
        {
            result = await userManager.RemoveFromRoleAsync(user, AppRoles.Administrator);
        }

        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = string.Join(" ", result.Errors.Select(error => error.Description));
            return RedirectToPage();
        }

        await userManager.UpdateSecurityStampAsync(user);
        TempData["StatusMessage"] = enabled
            ? "Пользователю назначена роль администратора."
            : "Роль администратора снята.";
        return RedirectToPage();
    }

    private async Task LoadUsersAsync()
    {
        var users = await userManager.Users
            .AsNoTracking()
            .OrderBy(user => user.Email ?? user.UserName)
            .ToListAsync();

        var rows = new List<UserRoleRow>(users.Count);
        foreach (var user in users)
        {
            rows.Add(new UserRoleRow(
                user.Id,
                user.Email ?? user.UserName ?? user.Id,
                await userManager.IsInRoleAsync(user, AppRoles.Administrator),
                user.Id == currentUser.Id));
        }

        Users = rows;
    }
}

public sealed record UserRoleRow(
    string Id,
    string DisplayName,
    bool IsAdministrator,
    bool IsCurrentUser);
