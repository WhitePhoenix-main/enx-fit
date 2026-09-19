using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Security.Claims;

namespace enx_fit.Security;

public static class UserRoleExtensions
{
    public const string RoleClaimType = "enx-fit:role-level";

    public static UserRole? GetUserRole(this ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
            return null;

        var value = principal.FindFirstValue(RoleClaimType);
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var level)
            && Enum.IsDefined((UserRole)level) ? (UserRole)level : null;
    }

    public static bool HasMinimumRole(this ClaimsPrincipal principal, UserRole minimum) =>
        Enum.IsDefined(minimum) && principal.GetUserRole() is { } role && role >= minimum;

    public static string DisplayName(this UserRole role) =>
        typeof(UserRole).GetField(role.ToString())?.GetCustomAttribute<DisplayAttribute>()?.GetName()
        ?? role.ToString();
}
