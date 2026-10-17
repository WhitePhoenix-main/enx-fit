using enx_fit.Pages.Infrastructure;
using enx_fit.Security;
using enx_fit.Services.Accounts;
using enx_fit.Services.Body;
using enx_fit.ViewModels.Body;
using Microsoft.AspNetCore.Mvc;

namespace enx_fit.Pages.Progress.Measurements;

[MinimumRole(UserRole.User)]
public class EditModel(
    BodyMeasurementService measurementService,
    CurrentUser currentUser,
    UserDirectoryService userDirectory) : OwnerFormPageModel(currentUser, userDirectory)
{
    [BindProperty]
    public int Id { get; set; }

    [BindProperty]
    public BodyMeasurementInputModel Input { get; set; } = new();

    public DateOnly Today => Viewer.LocalToday;

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
        if (!await OwnerIsValidAsync(Input.OwnerId) || !ModelState.IsValid)
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
}
