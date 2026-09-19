using enx_fit.Admin;
using enx_fit.Areas.Identity.Data;
using enx_fit.Data;
using enx_fit.Extensions;
using enx_fit.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace enx_fit.Pages.Admin.Users;

public static class CalendarPageUrls
{
    public static string CreateUserUrl(this IUrlHelper url)
        => url.PageUrl("/Admin/Users/CreateUser", nameof(CreateUserModel.OnGet));
}   
[MinimumRole(UserRole.Administrator)]
public class IndexModel(ApplicationDbContext db, CurrentUser currentUser) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    [BindProperty(SupportsGet = true)] public UserRole? Role { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    public const int PageSize = 12;
    public int TotalUsers { get; private set; }
    public int AdministratorCount { get; private set; }
    public int FilteredCount { get; private set; }
    public int PageCount => Math.Max(1, (int)Math.Ceiling(FilteredCount / (double)PageSize));
    public IReadOnlyList<UserRow> Users { get; private set; } = [];
    public async Task OnGetAsync()
    {
        TotalUsers = await db.Users.CountAsync();
        AdministratorCount = await db.Users.CountAsync(u => u.Role >= UserRole.Administrator);
        var query = db.Users.AsNoTracking();
        Search = Search?.Trim();
        if (!string.IsNullOrWhiteSpace(Search))
        {
            var search = Search.ToUpperInvariant();
            query = query.Where(u => (u.NormalizedEmail != null && u.NormalizedEmail.Contains(search)) || (u.NormalizedUserName != null && u.NormalizedUserName.Contains(search)));
        }
        if (Role is { } role && Enum.IsDefined(role)) query = query.Where(u => u.Role == role);
        else Role = null;
        FilteredCount = await query.CountAsync();
        PageNumber = Math.Clamp(PageNumber, 1, PageCount);
        var users = await query.OrderBy(u => u.Email ?? u.UserName).ThenBy(u => u.Id).Skip((PageNumber - 1) * PageSize).Take(PageSize)
            .Select(u => new { u.Id, u.UserName, u.Email, u.EmailConfirmed, u.Role }).ToListAsync();
        var ids = users.Select(u => u.Id).ToArray();
        var workouts = await db.WorkoutSessions.Where(w => w.UserId != null && ids.Contains(w.UserId)).GroupBy(w => w.UserId!)
            .Select(g => new { Id = g.Key, Count = g.Count(), Last = g.Max(w => w.Date) }).ToDictionaryAsync(g => g.Id);
        Users = users.Select(u => new UserRow(u.Id, u.UserName ?? u.Email ?? u.Id, u.Email, u.EmailConfirmed, u.Role, u.Id == currentUser.Id,
            workouts.GetValueOrDefault(u.Id)?.Count ?? 0, workouts.GetValueOrDefault(u.Id)?.Last)).ToList();
    }
}
public sealed record UserRow(string Id, string Name, string? Email, bool EmailConfirmed, UserRole Role, bool IsCurrentUser, int Workouts, DateOnly? LastWorkout)
{
    public bool IsAdministrator => Role >= UserRole.Administrator;
}
