using enx_fit.Security;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AdminSmoke.Pages.RoleChecks;

[MinimumRole(UserRole.Trainer)]
public class TrainerModel : PageModel
{
    public void OnGet() { }
    public void OnPost() { }
}

[MinimumRole(UserRole.SuperAdministrator)]
public class SuperModel : PageModel
{
    public void OnGet() { }
}

[MinimumRole(UserRole.Guest)]
public class GuestModel : PageModel
{
    public void OnGet() { }
}
