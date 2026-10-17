using enx_fit.Areas.Identity.Data;
using enx_fit.Data;
using Microsoft.EntityFrameworkCore;

namespace enx_fit.Security;

// Scoped to one HTTP request. Role changes are read again on the next request.
public sealed class UserAccountReader(ApplicationDbContext db)
{
    private readonly Dictionary<string, Task<ApplicationUser?>> accounts = new();

    public Task<ApplicationUser?> GetAsync(string userId)
    {
        if (!accounts.TryGetValue(userId, out var account))
        {
            account = db.Users.AsNoTracking().SingleOrDefaultAsync(user => user.Id == userId);
            accounts.Add(userId, account);
        }
        return account;
    }
}
