using Microsoft.AspNetCore.Razor.TagHelpers;

namespace enx_fit.TagHelpers;

[HtmlTargetElement("admin-icon")]
public sealed class AdminIconTagHelper : TagHelper
{
    public string Name { get; set; } = "grid";
    private static readonly Dictionary<string, string> Paths = new()
    {
        ["grid"] = "<rect x='3' y='3' width='7' height='7' rx='1.5'/><rect x='14' y='3' width='7' height='7' rx='1.5'/><rect x='3' y='14' width='7' height='7' rx='1.5'/><rect x='14' y='14' width='7' height='7' rx='1.5'/>",
        ["users"] = "<circle cx='9' cy='8' r='3'/><path d='M3 21v-3a6 6 0 0 1 12 0v3M16 5a3 3 0 0 1 0 6m3 10v-3a6 6 0 0 0-3-5'/>",
        ["workout"] = "<path d='M3 9v6m3-9v12m12-12v12m3-9v6M6 12h12'/>",
        ["book"] = "<path d='M12 5v16M3 3h5a4 4 0 0 1 4 2 4 4 0 0 1 4-2h5v16h-5a4 4 0 0 0-4 2 4 4 0 0 0-4-2H3Z'/>",
        ["activity"] = "<path d='M2 12h5l3-8 4 16 3-8h5'/>",
        ["search"] = "<circle cx='10.5' cy='10.5' r='6.5'/><path d='m16 16 5 5'/>",
        ["plus"] = "<path d='M12 5v14M5 12h14'/>",
        ["arrow"] = "<path d='M4 12h16m-6-6 6 6-6 6'/>",
        ["back"] = "<path d='M20 12H4m6-6-6 6 6 6'/>",
        ["menu"] = "<path d='M4 6h16M4 12h16M4 18h16'/>",
        ["calendar"] = "<rect x='3' y='5' width='18' height='16' rx='2'/><path d='M7 3v4m10-4v4M3 11h18m-13 4h2m4 0h2'/>",
        ["shield"] = "<path d='m12 3 8 3v6c0 5-8 9-8 9s-8-4-8-9V6Zm-4 9 3 3 5-6'/>",
        ["edit"] = "<path d='m15 4 5 5M4 20l5-1L21 7a2 2 0 0 0-4-4L5 15Zm9 0h8'/>",
        ["logout"] = "<path d='M9 4H4v16h5m6-13 5 5-5 5M8 12h12'/>",
        ["check"] = "<path d='m5 12 4 4L19 6'/>",
        ["chevron"] = "<path d='m9 5 7 7-7 7'/>",
        ["trend"] = "<path d='m3 17 6-6 4 4 8-10m-6 0h6v6'/>",
        ["clock"] = "<circle cx='12' cy='12' r='9'/><path d='M12 7v5l3 2'/>"
    };

    public override void Process(TagHelperContext context, TagHelperOutput output)
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
        output.Content.SetHtmlContent(Paths.GetValueOrDefault(Name, Paths["grid"]));
    }
}
