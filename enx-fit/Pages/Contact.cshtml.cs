using System.Net.Mail;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Pages;

public sealed class ContactModel(IConfiguration configuration) : PageModel
{
    public string Email { get; private set; } = "support@enixfit.test";
    public bool IsTest { get; private set; } = true;
    public void OnGet()
    {
        var candidate = configuration["PublicContact:Email"]?.Trim();
        if (MailAddress.TryCreate(candidate, out var address) && address.Address == candidate) Email = address.Address;
        IsTest = configuration.GetValue("PublicContact:IsTest", true) || Email.EndsWith(".test", StringComparison.OrdinalIgnoreCase)
            || Email.EndsWith(".example", StringComparison.OrdinalIgnoreCase);
    }
}
