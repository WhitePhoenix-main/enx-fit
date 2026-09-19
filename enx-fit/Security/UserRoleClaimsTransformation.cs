using System.Globalization;
using System.Security.Claims;
using enx_fit.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace enx_fit.Security;

/// <summary>Uses the database role on every request, including requests with an older cookie.</summary>
public sealed class UserRoleClaimsTransformation(ApplicationDbContext db) : IClaimsTransformation
{
    private readonly Dictionary<string, UserRole?> roles = new();

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
            return principal;

        var result = principal.Clone();
        foreach (var claimsIdentity in result.Identities)
        {
            foreach (var claim in claimsIdentity.Claims.Where(claim =>
                claim.Type == UserRoleExtensions.RoleClaimType ||
                (claim.Type == claimsIdentity.RoleClaimType && claim.Value == AppRoles.Administrator)).ToList())
                claimsIdentity.RemoveClaim(claim);
        }

        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return result;

        if (!roles.TryGetValue(userId, out var role))
        {
            role = await db.Users.AsNoTracking()
                .Where(user => user.Id == userId)
                .Select(user => (UserRole?)user.Role)
                .SingleOrDefaultAsync();
            roles[userId] = role;
        }

        if (role is null || !Enum.IsDefined(role.Value))
            return result;

        var target = result.Identities.First(item => item.IsAuthenticated);
        target.AddClaim(new Claim(UserRoleExtensions.RoleClaimType,
            ((int)role.Value).ToString(CultureInfo.InvariantCulture)));
        // Compatibility for existing views and Identity role-based checks.
        if (role >= UserRole.Administrator)
            target.AddClaim(new Claim(target.RoleClaimType, AppRoles.Administrator));

        return result;
    }
}
