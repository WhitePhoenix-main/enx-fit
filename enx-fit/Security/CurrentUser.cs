using System.Security.Claims;

namespace enx_fit.Security;

public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor)
{
    private ClaimsPrincipal Principal => httpContextAccessor.HttpContext?.User
        ?? throw new InvalidOperationException("No active HTTP request is available.");

    public string Id => Principal.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("The current request is not authenticated.");

    public UserRole? Role => Principal.GetUserRole();
    public int UtcOffsetMinutes => int.TryParse(httpContextAccessor.HttpContext?.Request.Cookies["enix-utc-offset"], out var offset) && offset is >= -840 and <= 840 ? offset : 0;
    public DateOnly LocalToday => DateOnly.FromDateTime(DateTime.UtcNow.AddMinutes(UtcOffsetMinutes));

    public bool HasMinimumRole(UserRole minimum) => Principal.HasMinimumRole(minimum);

    public bool IsAdministrator => HasMinimumRole(UserRole.Administrator);

    public string ResolveOwnerId(string? requestedOwnerId) =>
        IsAdministrator && !string.IsNullOrWhiteSpace(requestedOwnerId)
            ? requestedOwnerId
            : Id;
}
