using enx_fit.Security;
using enx_fit.Services;
using enx_fit.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Pages.Exercises;

[MinimumRole(UserRole.Administrator)]
public class EditModel(ExerciseService exerciseService) : PageModel
{
    [BindProperty]
    public int Id { get; set; }

    [BindProperty]
    public ExerciseInputModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var exercise = await exerciseService.FindAsync(id);

        if (exercise is null)
        {
            return NotFound();
        }

        Id = exercise.Id;
        Input = ExerciseInputModel.FromExercise(exercise);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await exerciseService.UpdateAsync(Id, Input);

        if (result == ExerciseWriteResult.NotFound)
        {
            return NotFound();
        }

        if (result == ExerciseWriteResult.DuplicateName)
        {
            ModelState.AddModelError("Input.Name", "Упражнение с таким названием уже существует.");
            return Page();
        }

        TempData["StatusMessage"] = "Упражнение обновлено.";
        return RedirectToPage("./Index");
    }
}
