using System.Data;
using enx_fit.Areas.Identity.Data;
using enx_fit.Data;
using enx_fit.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace enx_fit.Services;

public sealed class RegistrationService(ApplicationDbContext db, UserManager<ApplicationUser> users)
{
    public async Task<IdentityResult> RegisterAsync(
        ApplicationUser user, string? password = null, UserLoginInfo? externalLogin = null)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            db.Database.IsNpgsql() ? IsolationLevel.ReadCommitted : IsolationLevel.Serializable);

        // Serialize the empty-table check and insertion across application instances.
        // This lock also excludes inserts made outside the registration service.
        if (db.Database.IsNpgsql())
            await db.Database.ExecuteSqlRawAsync("LOCK TABLE \"AspNetUsers\" IN SHARE ROW EXCLUSIVE MODE");

        user.Role = await db.Users.AnyAsync() ? UserRole.User : UserRole.Administrator;
        var result = password is null
            ? await users.CreateAsync(user)
            : await users.CreateAsync(user, password);

        if (result.Succeeded && externalLogin is not null)
            result = await users.AddLoginAsync(user, externalLogin);

        if (result.Succeeded)
            result = await users.AddToRolesAsync(user, user.Role == UserRole.Administrator
                ? AppRoles.All : [AppRoles.User]);

        if (result.Succeeded)
            await transaction.CommitAsync();
        else
        {
            await transaction.RollbackAsync();
            db.ChangeTracker.Clear();
        }

        return result;
    }
}
