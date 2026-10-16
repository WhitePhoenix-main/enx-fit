using enx_fit.Security;
using enx_fit.Services.Accounts;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Pages.Infrastructure;

public abstract class OwnerFormPageModel(CurrentUser currentUser, UserDirectoryService userDirectory) : PageModel
{
    protected CurrentUser Viewer => currentUser;
    protected UserDirectoryService UserDirectory => userDirectory;

    public bool IsAdministrator => currentUser.IsAdministrator;
    public IReadOnlyList<UserOption> Users { get; private set; } = [];

    protected async Task<bool> OwnerIsValidAsync(string? ownerId)
    {
        if (!IsAdministrator || await userDirectory.ExistsAsync(ownerId))
            return true;

        ModelState.AddModelError("Input.OwnerId", "Выберите существующего пользователя.");
        return false;
    }

    protected async Task LoadUsersAsync()
    {
        if (IsAdministrator)
            Users = await userDirectory.GetAllAsync();
    }
}
