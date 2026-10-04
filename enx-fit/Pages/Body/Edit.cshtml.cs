using enx_fit.Services;
using enx_fit.Security;
using enx_fit.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Pages.Body;

[MinimumRole(UserRole.User)]
public class EditModel(
    BodyMeasurementService measurementService,
    CurrentUser currentUser,
    UserDirectoryService userDirectory) : PageModel
{
    [BindProperty]
    public int Id { get; set; }

    [BindProperty]
    public BodyMeasurementInputModel Input { get; set; } = new();

    public bool IsAdministrator => currentUser.IsAdministrator;
    public DateOnly Today => currentUser.LocalToday;

    public IReadOnlyList<UserOption> Users { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (Request.Query.ContainsKey("ClientId")) return NotFound();
        var measurement = await measurementService.FindAsync(id);

        if (measurement is null)
        {
            return NotFound();
        }

        Id = measurement.Id;
        Input = BodyMeasurementInputModel.FromMeasurement(measurement);
        await LoadUsersAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Request.Query.ContainsKey("ClientId")) return NotFound();
        if (await measurementService.FindAsync(Id) is null) return NotFound();
        BodyFormValidation.Validate(Input, ModelState, Today);
        if (!await OwnerIsValidAsync() || !ModelState.IsValid)
        {
            await LoadUsersAsync();
            return Page();
        }

        if (!await measurementService.UpdateAsync(Id, Input))
        {
            return NotFound();
        }

        TempData["StatusMessage"] = "Замер обновлён.";
        return RedirectToPage("./Index", new { UserId = IsAdministrator ? Input.OwnerId : null });
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
