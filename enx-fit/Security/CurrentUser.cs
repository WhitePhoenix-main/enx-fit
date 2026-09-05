using System.Security.Claims;

namespace enx_fit.Security;

public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor)
{
    private ClaimsPrincipal Principal => httpContextAccessor.HttpContext?.User
        ?? throw new InvalidOperationException("No active HTTP request is available.");

    public string Id => Principal.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("The current request is not authenticated.");

    public bool IsAdministrator => Principal.IsInRole(AppRoles.Administrator);

    public string ResolveOwnerId(string? requestedOwnerId) =>
        IsAdministrator && !string.IsNullOrWhiteSpace(requestedOwnerId)
            ? requestedOwnerId
            : Id;
}
