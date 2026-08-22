using enx_fit.Services;
using enx_fit.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Pages.Body;

public class EditModel(BodyMeasurementService measurementService) : PageModel
{
    [BindProperty]
    public int Id { get; set; }

    [BindProperty]
    public BodyMeasurementInputModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var measurement = await measurementService.FindAsync(id);

        if (measurement is null)
        {
            return NotFound();
        }

        Id = measurement.Id;
        Input = BodyMeasurementInputModel.FromMeasurement(measurement);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (!await measurementService.UpdateAsync(Id, Input))
        {
            return NotFound();
        }

        TempData["StatusMessage"] = "Body measurement updated.";
        return RedirectToPage("./Index");
    }
}
