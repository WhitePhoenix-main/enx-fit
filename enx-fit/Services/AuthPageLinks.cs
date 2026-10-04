using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace enx_fit.Services;

public static class AuthPageLinks
{
    public static string LocalReturnUrl(IUrlHelper url, string? destination) =>
        url.IsLocalUrl(destination) ? destination! : url.Content("~/Dashboard");

    public static bool TryDecodeToken(string? encoded, out string token)
    {
        token = "";
        if (string.IsNullOrWhiteSpace(encoded) || encoded.Length > 8192) return false;
        try { token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encoded)); return token.Length > 0; }
        catch (FormatException) { return false; }
    }
}
