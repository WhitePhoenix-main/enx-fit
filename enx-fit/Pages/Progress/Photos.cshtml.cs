using enx_fit.Data;
using enx_fit.Models;
using enx_fit.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace enx_fit.Pages.Progress;

[MinimumRole(UserRole.User)]
[RequestSizeLimit(6_000_000)]
public sealed class PhotosModel(ApplicationDbContext db, CurrentUser user, IFeatureAccessService features) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? ClientId { get; set; }
    [BindProperty] public IFormFile? Photo { get; set; }
    [BindProperty] public DateOnly Date { get; set; }
    public string SubjectId => ClientId ?? user.Id;
    public string SubjectName { get; private set; } = "";
    public IReadOnlyList<(int Id, DateOnly Date)> Photos { get; private set; } = [];
    private async Task<bool> AllowedAsync() => await features.CanUseAsync(Feature.AnalyticsAdvanced) &&
        (SubjectId == user.Id || await features.CanUseAsync(Feature.CoachAnalytics) &&
            await db.Users.AnyAsync(c => c.Id == SubjectId && c.TrainerId == user.Id));
    public async Task<IActionResult> OnGetAsync()
    {
        if (!await AllowedAsync()) return NotFound();
        SubjectName = await db.Users.Where(u => u.Id == SubjectId).Select(u => u.UserName ?? "Спортсмен").SingleAsync();
        var rows = await db.ProgressPhotos.AsNoTracking().Where(p => p.UserId == SubjectId).OrderBy(p => p.Date).ThenBy(p => p.Id)
            .Select(p => new { p.Id, p.Date }).ToListAsync();
        Photos = rows.Select(p => (p.Id, p.Date)).ToArray(); Date = user.LocalToday;
        Response.Headers.CacheControl = "no-store"; return Page();
    }
    public async Task<IActionResult> OnGetImageAsync(int id)
    {
        if (!await AllowedAsync()) return NotFound();
        var photo = await db.ProgressPhotos.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id && p.UserId == SubjectId);
        if (photo is null) return NotFound();
        Response.Headers.CacheControl = "private, no-store"; Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(photo.Content, photo.ContentType);
    }
    public async Task<IActionResult> OnPostAsync()
    {
        if (!await AllowedAsync()) return NotFound();
        if (Photo is null || Photo.Length is < 12 or > 5_000_000) ModelState.AddModelError("Photo", "Выберите JPEG или PNG размером до 5 МБ.");
        if (Date > user.LocalToday || Date < user.LocalToday.AddYears(-10)) ModelState.AddModelError("Date", "Проверьте дату фотографии.");
        if (await db.ProgressPhotos.CountAsync(p => p.UserId == SubjectId) >= 100) ModelState.AddModelError("Photo", "В альбоме уже 100 фотографий.");
        if (ModelState.IsValid)
        {
            using var bytes = new MemoryStream(); await Photo!.CopyToAsync(bytes); var data = bytes.ToArray();
            var type = data.AsSpan().StartsWith(new byte[] { 137,80,78,71,13,10,26,10 }) ? "image/png" :
                data.AsSpan().StartsWith(new byte[] { 255,216,255 }) ? "image/jpeg" : null;
            if (type is null) ModelState.AddModelError("Photo", "Поддерживаются только JPEG и PNG.");
            else { db.ProgressPhotos.Add(new() { UserId = SubjectId, Date = Date, Content = data, ContentType = type }); await db.SaveChangesAsync(); return RedirectToPage(new { ClientId }); }
        }
        var selected = Date; await OnGetAsync(); Date = selected; return Page();
    }
}
