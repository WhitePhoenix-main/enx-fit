using enx_fit.Security;
using enx_fit.Models;
using enx_fit.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Pages.Body;

[MinimumRole(UserRole.User)]
public class DeleteModel(BodyMeasurementService measurementService, CurrentUser currentUser) : PageModel
{
    [BindProperty]
    public BodyMeasurement Measurement { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id) =>
        await LoadPageAsync(id);

    public async Task<IActionResult> OnPostAsync()
    {
        if (Request.Query.ContainsKey("ClientId")) return NotFound();
        if (Measurement is null || Measurement.Id <= 0) return NotFound();
        var stored = await measurementService.FindAsync(Measurement.Id);
        if (stored is null) return NotFound();
        if (!await measurementService.DeleteAsync(Measurement.Id))
        {
            return NotFound();
        }

        TempData["StatusMessage"] = "Замер удалён.";
        return RedirectToPage("./Index", new { UserId = currentUser.IsAdministrator ? stored.UserId : null });
    }

    private async Task<IActionResult> LoadPageAsync(int id)
    {
        if (Request.Query.ContainsKey("ClientId")) return NotFound();
        var measurement = await measurementService.FindAsync(id);

        if (measurement is null)
        {
            return NotFound();
        }

        Measurement = measurement;
        return Page();
    }
}
