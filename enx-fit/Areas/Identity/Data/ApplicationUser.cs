using enx_fit.Security;
using Microsoft.AspNetCore.Identity;

namespace enx_fit.Areas.Identity.Data;

public class ApplicationUser : IdentityUser
{
    public ApplicationUser() { }

    public ApplicationUser(string userName) : base(userName) { }

    public UserRole Role { get; set; } = UserRole.User;
}
