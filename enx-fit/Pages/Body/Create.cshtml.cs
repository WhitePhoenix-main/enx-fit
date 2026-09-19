using enx_fit.Services;
using enx_fit.Security;
using enx_fit.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Pages.Body;

[MinimumRole(UserRole.User)]
public class CreateModel(
    BodyMeasurementService measurementService,
    CurrentUser currentUser,
    UserDirectoryService userDirectory) : PageModel
{
    [BindProperty]
    public BodyMeasurementInputModel Input { get; set; } = new();

    public bool IsAdministrator => currentUser.IsAdministrator;

    public IReadOnlyList<UserOption> Users { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        Input.OwnerId = currentUser.Id;
        await LoadUsersAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await OwnerIsValidAsync() || !ModelState.IsValid)
        {
            await LoadUsersAsync();
            return Page();
        }

        await measurementService.CreateAsync(Input);
        TempData["StatusMessage"] = "Замер добавлен.";
        return RedirectToPage("./Index");
    }

    private async Task<bool> OwnerIsValidAsync()
    {
        if (!IsAdministrator || await userDirectory.ExistsAsync(Input.OwnerId))
        {
            return true;
        }

        ModelState.AddModelError("Input.OwnerId", "Выберите существующего пользователя.");
        return false;
    }

    private async Task LoadUsersAsync()
    {
        if (IsAdministrator)
        {
            Users = await userDirectory.GetAllAsync();
        }
    }
}
