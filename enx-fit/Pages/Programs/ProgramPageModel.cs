using enx_fit.Security;
using enx_fit.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace enx_fit.Pages.Programs;

[MinimumRole(UserRole.User)]
public abstract class ProgramPageModel(TrainingProgramService programs) : PageModel
{
    protected TrainingProgramService Programs => programs;
    public FeatureAccess Access { get; protected set; } = null!;
    public string? PageError { get; protected set; }
    public bool ShowUpgrade { get; protected set; }
    protected async Task LoadAccessAsync()
    {
        Access = await programs.AccessAsync();
        Response.Headers.CacheControl = "no-cache, no-store";
    }
    protected IActionResult Error(ProgramOperationException e)
    {
        if (e.Failure is ProgramFailure.NotFound or ProgramFailure.Forbidden)
        {
            Response.StatusCode = e.Failure == ProgramFailure.NotFound ? 404 : 403;
            PageError = e.Failure == ProgramFailure.NotFound ? "Программа не найдена или недоступна." : "Эта функция недоступна для вашей роли или тарифа.";
        }
        else if (e.Failure is ProgramFailure.Limit or ProgramFailure.Upgrade) ShowUpgrade = true;
        else ModelState.AddModelError("", e.Failure == ProgramFailure.Conflict
            ? "Программа уже изменена в другом окне. Обновите страницу перед сохранением." : e.Message);
        return Page();
    }
    protected void SaveError(Exception error)
    {
        HttpContext.RequestServices.GetRequiredService<ILogger<ProgramPageModel>>().LogWarning(error, "Program could not be saved");
        ModelState.AddModelError("", error is DbUpdateConcurrencyException
            ? "Программа уже изменена. Обновите страницу и повторите сохранение."
            : "Не удалось сохранить изменения. Повторите попытку.");
    }
}
