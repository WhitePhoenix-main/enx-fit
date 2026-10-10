using enx_fit.Models;
using Microsoft.EntityFrameworkCore;

namespace enx_fit.Services;

public sealed record WorkoutDirectoryPage(IReadOnlyList<WorkoutSession> Items, int Count, int PageNumber, int PageCount);

public partial class WorkoutService
{
    public async Task<WorkoutDirectoryPage> DirectoryAsync(string? ownerId, DateOnly? date, int pageNumber)
    {
        var query = VisibleWorkouts().AsNoTracking();
        if (currentUser.IsAdministrator && !string.IsNullOrWhiteSpace(ownerId)) query = query.Where(w => w.UserId == ownerId);
        if (date.HasValue) query = query.Where(w => w.Date == date);
        var count = await query.CountAsync();
        var pageCount = Math.Max(1, (int)Math.Ceiling(count / 20d));
        pageNumber = Math.Clamp(pageNumber, 1, pageCount);
        var items = await query.AsSplitQuery().Include(w => w.WorkoutExercises).ThenInclude(e => e.SetEntries)
            .OrderByDescending(w => w.Date).ThenByDescending(w => w.Id).Skip((pageNumber - 1) * 20).Take(20).ToListAsync();
        return new(items, count, pageNumber, pageCount);
    }
}
