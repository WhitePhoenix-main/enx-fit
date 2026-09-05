using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace enx_fit.Services;

public sealed class UserDirectoryService(UserManager<IdentityUser> userManager)
{
    public async Task<IReadOnlyList<UserOption>> GetAllAsync() =>
        await userManager.Users
            .AsNoTracking()
            .OrderBy(user => user.Email ?? user.UserName)
            .Select(user => new UserOption(
                user.Id,
                user.Email ?? user.UserName ?? user.Id))
            .ToListAsync();

    public Task<bool> ExistsAsync(string? userId) =>
        string.IsNullOrWhiteSpace(userId)
            ? Task.FromResult(false)
            : userManager.Users.AnyAsync(user => user.Id == userId);

    public async Task<string> GetDisplayNameAsync(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return "Не назначен";
        }

        var user = await userManager.Users
            .AsNoTracking()
            .Where(candidate => candidate.Id == userId)
            .Select(candidate => candidate.Email ?? candidate.UserName)
            .SingleOrDefaultAsync();

        return user ?? "Удалённый пользователь";
    }

    public async Task<IReadOnlyDictionary<string, string>> GetDisplayNamesAsync(
        IEnumerable<string?> userIds)
    {
        var ids = userIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Cast<string>()
            .Distinct()
            .ToArray();

        return await userManager.Users
            .AsNoTracking()
            .Where(user => ids.Contains(user.Id))
            .ToDictionaryAsync(
                user => user.Id,
                user => user.Email ?? user.UserName ?? user.Id);
    }
}

public sealed record UserOption(string Id, string DisplayName);
