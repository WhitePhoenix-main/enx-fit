using enx_fit.Pages.Infrastructure;
using enx_fit.Security;
using enx_fit.Services.Accounts;
using enx_fit.Services.Body;
using enx_fit.ViewModels.Body;
using Microsoft.AspNetCore.Mvc;

namespace enx_fit.Pages.Progress.Measurements;

[MinimumRole(UserRole.User)]
public class CreateModel(
    BodyMeasurementService measurementService,
    CurrentUser currentUser,
    UserDirectoryService userDirectory) : OwnerFormPageModel(currentUser, userDirectory)
{
    [BindProperty]
    public BodyMeasurementInputModel Input { get; set; } = new();

    public DateOnly Today => Viewer.LocalToday;

    public async Task<IActionResult> OnGetAsync(string? ownerId = null)
    {
        if (Request.Query.ContainsKey("ClientId")) return NotFound();
        Input.OwnerId = IsAdministrator && !string.IsNullOrWhiteSpace(ownerId) ? ownerId : Viewer.Id;
        if (IsAdministrator && !await UserDirectory.ExistsAsync(Input.OwnerId)) return NotFound();
        Input.Date = Today;
        var latest = (await measurementService.GetAllAsync(Input.OwnerId)).FirstOrDefault(m => m.Date <= Today);
        Input.HeightCm = latest?.HeightCm ?? 0;
        await LoadUsersAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Request.Query.ContainsKey("ClientId")) return NotFound();
        BodyFormValidation.Validate(Input, ModelState, Today);
        if (!await OwnerIsValidAsync(Input.OwnerId) || !ModelState.IsValid)
        {
            await LoadUsersAsync();
            return Page();
        }

        await measurementService.CreateAsync(Input);
        TempData["StatusMessage"] = "Замер добавлен.";
        return RedirectToPage("./Index", new { UserId = IsAdministrator ? Input.OwnerId : null });
    }
}
