using Microsoft.AspNetCore.Authorization;

namespace enx_fit.Security;

/// <summary>Requires an authenticated user whose role is at least the specified level.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class MinimumRoleAttribute : AuthorizeAttribute
{
    public MinimumRoleAttribute(UserRole role)
    {
        if (!Enum.IsDefined(role))
            throw new ArgumentOutOfRangeException(nameof(role));

        Policy = PolicyName(role);
    }

    public static string PolicyName(UserRole role) => $"MinimumRole:{(int)role}";
}
