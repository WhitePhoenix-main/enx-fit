using enx_fit.Security;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Pages;

[MinimumRole(UserRole.User)]
public class DashboardModel : PageModel
{
    public void OnGet() { }
}
