using enx_fit.Services;
using enx_fit.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Pages.Workouts;

public class CreateModel(WorkoutService workoutService) : PageModel
{
    [BindProperty]
    public WorkoutSessionInputModel Input { get; set; } = new();

    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var id = await workoutService.CreateAsync(Input);
        TempData["StatusMessage"] = "Workout created.";
        return RedirectToPage("./Details", new { id });
    }
}
