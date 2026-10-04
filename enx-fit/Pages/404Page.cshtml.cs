using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class NotFoundModel : PageModel
{
    public int HttpStatus { get; private set; }
    public void OnGet()
    {
        HttpStatus = Response.StatusCode is >= 400 and <= 599 ? Response.StatusCode : 404;
        Response.StatusCode = HttpStatus;
    }
}
