using enx_fit.Data;
using enx_fit.Models;
using enx_fit.Security;
using Microsoft.EntityFrameworkCore;

namespace enx_fit.Services.Coaching;

public sealed record MessageContact(string Id, string Name);
public sealed class CoachMessageService(ApplicationDbContext db, CurrentUser user, IFeatureAccessService features)
{
    public async Task<IReadOnlyList<MessageContact>> ContactsAsync()
    {
        if (await features.CanUseAsync(Feature.CoachClientManagement))
            return await db.Users.AsNoTracking().Where(u => u.TrainerId == user.Id && u.Id != user.Id).OrderBy(u => u.UserName).Select(u => new MessageContact(u.Id, u.UserName ?? "Клиент")).ToListAsync();
        var trainer = await db.Users.Where(u => u.Id == user.Id).Select(u => u.TrainerId).SingleAsync();
        return trainer is null ? [] : await db.Users.AsNoTracking().Where(u => u.Id == trainer).Select(u => new MessageContact(u.Id, u.UserName ?? "Тренер")).ToListAsync();
    }
    public async Task<IReadOnlyList<CoachMessage>> ThreadAsync(string contactId)
    {
        if (!(await ContactsAsync()).Any(c => c.Id == contactId)) return [];
        return (await db.Set<CoachMessage>().AsNoTracking().Where(m => m.SenderId == user.Id && m.RecipientId == contactId || m.SenderId == contactId && m.RecipientId == user.Id)
            .OrderByDescending(m => m.CreatedAtUtc).ThenByDescending(m => m.Id).Take(100).ToListAsync()).OrderBy(m => m.CreatedAtUtc).ThenBy(m => m.Id).ToArray();
    }
    public async Task<bool> SendAsync(string contactId, string text, Guid requestId)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Trim().Length > 2000 || requestId == Guid.Empty) return false;
        if (!(await ContactsAsync()).Any(c => c.Id == contactId)) return false;
        if (await db.Set<CoachMessage>().AnyAsync(m => m.SenderId == user.Id && m.RequestId == requestId)) return true;
        db.Add(new CoachMessage { SenderId = user.Id, RecipientId = contactId, Text = text.Trim(), RequestId = requestId });
        await db.SaveChangesAsync(); return true;
    }
}
