using enx_fit.Security;
using enx_fit.Models;
using enx_fit.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Pages.Exercises;

[MinimumRole(UserRole.User)]
public class DetailsModel(ExerciseService exerciseService) : PageModel
{
    public Exercise Exercise { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var exercise = await exerciseService.FindAsync(id);

        if (exercise is null)
        {
            return NotFound();
        }

        Exercise = exercise;
        return Page();
    }
}
