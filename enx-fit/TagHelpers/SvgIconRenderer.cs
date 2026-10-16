using Microsoft.AspNetCore.Razor.TagHelpers;

namespace enx_fit.TagHelpers;

internal static class SvgIconRenderer
{
    private static readonly IReadOnlyDictionary<string, string> SharedPaths = new Dictionary<string, string>
    {
        ["bolt"] = "<path d='m13 2-8 12h6l-1 8 9-13h-6l1-7Z'/>",
        ["document"] = "<path d='M14 3H5v18h14V8Zm0 0v5h5M8 12h8m-8 4h8'/>",
        ["repeat"] = "<path d='M20 8a8 8 0 0 0-14-3L3 8m0-5v5h5M4 16a8 8 0 0 0 14 3l3-3m0 5v-5h-5'/>",
        ["coffee"] = "<path d='M4 6h12v14H4Zm12 2h3a3 3 0 0 1 0 6h-3M7 2h5'/>",
        ["settings"] = "<path d='m10 2-1 3-3 1-3-1-2 4 3 2v3l-2 2 2 4 3-1 3 1 1 3h4l1-3 3-1 3 1 2-4-3-2v-3l2-2-2-4-3 1-3-1-1-3Z'/><circle cx='12' cy='12' r='3'/>",
        ["close"] = "<path d='m5 5 14 14M5 19 19 5'/>",
        ["users"] = "<circle cx='9' cy='8' r='3'/><path d='M3 21v-3a6 6 0 0 1 12 0v3M16 5a3 3 0 0 1 0 6m3 10v-3a6 6 0 0 0-3-5'/>",
        ["book"] = "<path d='M12 5v16M3 3h5a4 4 0 0 1 4 2 4 4 0 0 1 4-2h5v16h-5a4 4 0 0 0-4 2 4 4 0 0 0-4-2H3Z'/>",
        ["arrow"] = "<path d='M4 12h16m-6-6 6 6-6 6'/>",
        ["menu"] = "<path d='M4 6h16M4 12h16M4 18h16'/>",
        ["check"] = "<path d='m5 12 4 4L19 6'/>",
        ["chevron"] = "<path d='m9 5 7 7-7 7'/>",
        ["trend"] = "<path d='m3 17 6-6 4 4 8-10m-6 0h6v6'/>",
    };

    public static void Render(TagHelperOutput output, IReadOnlyDictionary<string, string> theme, string name, string fallback)
    {
        output.TagName = "svg";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("viewBox", "0 0 24 24");
        output.Attributes.SetAttribute("fill", "none");
        output.Attributes.SetAttribute("stroke", "currentColor");
        output.Attributes.SetAttribute("stroke-width", "1.7");
        output.Attributes.SetAttribute("stroke-linecap", "round");
        output.Attributes.SetAttribute("stroke-linejoin", "round");
        output.Attributes.SetAttribute("aria-hidden", "true");
        output.Attributes.SetAttribute("class", "admin-icon");
        output.Content.SetHtmlContent(theme.GetValueOrDefault(name) ?? SharedPaths.GetValueOrDefault(name) ?? theme[fallback]);
    }
}
