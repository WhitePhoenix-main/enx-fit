using enx_fit.Security;
using enx_fit.Models;
using enx_fit.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Pages.Body;

[MinimumRole(UserRole.User)]
public class DeleteModel(BodyMeasurementService measurementService) : PageModel
{
    [BindProperty]
    public BodyMeasurement Measurement { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id) =>
        await LoadPageAsync(id);

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await measurementService.DeleteAsync(Measurement.Id))
        {
            return NotFound();
        }

        TempData["StatusMessage"] = "Замер удалён.";
        return RedirectToPage("./Index");
    }

    private async Task<IActionResult> LoadPageAsync(int id)
    {
        var measurement = await measurementService.FindAsync(id);

        if (measurement is null)
        {
            return NotFound();
        }

        Measurement = measurement;
        return Page();
    }
}
