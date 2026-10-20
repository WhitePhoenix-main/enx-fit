using System.ComponentModel.DataAnnotations;
using enx_fit.Models;
using enx_fit.Security;
using enx_fit.Services.Coaching;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Pages.Coaching;

[MinimumRole(UserRole.User)]
[RequestSizeLimit(16384)]
public sealed class MessagesModel(CoachMessageService messages, CurrentUser user) : PageModel
{
    [BindProperty(SupportsGet = true), StringLength(450)] public string? ContactId { get; set; }
    [BindProperty, Required, StringLength(2000)] public string Text { get; set; } = "";
    [BindProperty] public Guid RequestId { get; set; } = Guid.NewGuid();
    public IReadOnlyList<MessageContact> Contacts { get; private set; } = [];
    public IReadOnlyList<CoachMessage> Thread { get; private set; } = [];
    public string ViewerId => user.Id;
    public int Offset => user.UtcOffsetMinutes;
    public async Task<IActionResult> OnGetAsync()
    {
        Contacts = await messages.ContactsAsync();
        if (ContactId is not null && !Contacts.Any(c => c.Id == ContactId)) return NotFound();
        ContactId ??= Contacts.FirstOrDefault()?.Id;
        if (ContactId is not null) Thread = await messages.ThreadAsync(ContactId);
        Response.Headers.CacheControl = "no-store";
        return Page();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        if (ModelState.IsValid && ContactId is not null && await messages.SendAsync(ContactId, Text, RequestId))
            return RedirectToPage(new { ContactId });
        ModelState.AddModelError("", "Сообщение не отправлено. Проверьте получателя и текст.");
        return await OnGetAsync();
    }
}
