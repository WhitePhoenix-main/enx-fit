using Microsoft.AspNetCore.Identity.UI.Services;

namespace enx_fit.Services;

public static class AccountEmailDelivery
{
    // Identity's default sender returns successfully without delivering a message.
    public static bool IsAvailable(IEmailSender sender) =>
        sender.GetType().Assembly != typeof(IEmailSender).Assembly || sender.GetType().Name != "NoOpEmailSender";

    public static async Task<bool> TrySendAsync(IEmailSender sender, ILogger logger,
        string email, string subject, string body, CancellationToken cancellationToken)
    {
        if (!IsAvailable(sender)) return false;
        try { await sender.SendEmailAsync(email, subject, body); return true; }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Account email could not be delivered.");
            return false;
        }
    }
}
