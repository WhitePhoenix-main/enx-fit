using Microsoft.AspNetCore.Razor.TagHelpers;

namespace enx_fit.TagHelpers;

[HtmlTargetElement("admin-icon")]
public sealed class AdminIconTagHelper : TagHelper
{
    public string Name { get; set; } = "grid";
    private static readonly Dictionary<string, string> Paths = new()
    {
        ["grip"] = "<circle cx='8' cy='5' r='1'/><circle cx='16' cy='5' r='1'/><circle cx='8' cy='12' r='1'/><circle cx='16' cy='12' r='1'/><circle cx='8' cy='19' r='1'/><circle cx='16' cy='19' r='1'/>",
        ["up"] = "<path d='M12 20V4m-6 6 6-6 6 6'/>",
        ["down"] = "<path d='M12 4v16m-6-6 6 6 6-6'/>",
        ["home"] = "<path d='m3 10 9-7 9 7v10a1 1 0 0 1-1 1h-5v-7H9v7H4a1 1 0 0 1-1-1Z'/>",
        ["bars"] = "<path d='M5 20v-6m7 6V8m7 12V3' stroke-width='3'/>",
        ["bell"] = "<path d='M18 8a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9M10 21h4'/>",
        ["target"] = "<circle cx='11' cy='13' r='8'/><circle cx='11' cy='13' r='4'/><path d='m11 13 9-10m-5 1 5-1v5'/>",
        ["moon"] = "<path d='M20 14A9 9 0 0 1 10 3a9 9 0 1 0 10 11Z'/>",
        ["water"] = "<path d='M12 2S5 10 5 15a7 7 0 0 0 14 0c0-5-7-13-7-13Zm-4 13a4 4 0 0 0 4 4'/>",
        ["flame"] = "<path d='M12 2c2 6-3 7-1 11 2-1 3-3 4-5 5 5 6 9 2 12a8 8 0 0 1-10 0C1 16 6 8 8 7c-1 5 0 6 1 6-1-6 4-7 3-11Z'/>",
        ["trophy"] = "<path d='M7 3h10v7a5 5 0 0 1-10 0Zm0 2H3v3a4 4 0 0 0 4 4m10-7h4v3a4 4 0 0 1-4 4m-5 3v6m-5 0h10'/>",
        ["spark"] = "<path d='m12 3 3 7 7 2-7 3-3 7-3-7-7-3 7-2Zm7-2 1 3 3 1-3 1-1 3-1-3-3-1 3-1Z'/>",
        ["nutrition"] = "<path d='M4 3v6a3 3 0 0 0 6 0V3M7 3v18m10 0V3c-4 4-4 10 0 10h2'/>",
        ["shoe"] = "<path d='m5 3 4 1 2 8 9 4 1 5H3L2 9Zm6 9-4 2m8 0-4 2'/>",
        ["scale"] = "<rect x='3' y='3' width='18' height='18' rx='4'/><path d='M7 7a8 8 0 0 1 10 0l-2 4H9Zm5 0v4'/>",
        ["settings"] = "<path d='m10 2-1 3-3 1-3-1-2 4 3 2v3l-2 2 2 4 3-1 3 1 1 3h4l1-3 3-1 3 1 2-4-3-2v-3l2-2-2-4-3 1-3-1-1-3Z'/><circle cx='12' cy='12' r='3'/>",
        ["user"] = "<circle cx='12' cy='7' r='4'/><path d='M4 22v-3a8 8 0 0 1 16 0v3'/>",
        ["play"] = "<path d='m8 4 12 8-12 8Z' fill='currentColor' stroke='none'/>",
        ["close"] = "<path d='m5 5 14 14M5 19 19 5'/>",
        ["info"] = "<circle cx='12' cy='12' r='9'/><path d='M12 11v6m0-10v.1'/>",
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
        ["clock"] = "<circle cx='12' cy='12' r='9'/><path d='M12 7v5l3 2'/>",
        ["chest"] = "<circle cx='12' cy='4' r='2'/><path d='m8 8 4 2 4-2M8 8l-4 3-2-3m14 0 4 3 2-3M8 8v8l4 2 4-2V8M9 18l-1 4m7-4 1 4M12 10v6'/>",
        ["back-muscles"] = "<circle cx='12' cy='4' r='2'/><path d='M3 3v5l5 3v6h8v-6l5-3V3M8 10l4 3 4-3m-4 3v4m-3 0-1 5m7-5 1 5M1 2h22'/>",
        ["shoulders"] = "<circle cx='12' cy='5' r='2'/><path d='M8 10h8v7H8Zm0 1L4 8V3m12 8 4-3V3M2 3h4m12 0h4M9 17l-1 5m7-5 1 5'/>",
        ["legs"] = "<circle cx='12' cy='3' r='2'/><path d='m9 7-1 7 5 2-2 6m4-15 1 7 4 3-3 5M8 9l-4 3m12-3 4 3M9 7h6'/>",
        ["core"] = "<circle cx='12' cy='4' r='2'/><path d='M8 9h8l1 8H7l1-8Zm4 0v8m-3 1-1 4m7-4 1 4'/>",
        ["machine"] = "<rect x='3' y='3' width='5' height='17' rx='1'/><path d='M3 8h5M3 13h5M3 18h5M8 6h8l3 4v10M16 6v8m-7 0h8M10 14v6'/>",
        ["cable"] = "<path d='M4 20V4h16v16M4 8h5m11 0h-5M9 8l3 6 3-6m-3 6v5'/>",
        ["bench"] = "<path d='M3 13h14v3H3zm2 3-2 5m12-5 2 5m1-10 3-2v7h-3z'/>",
        ["barbell"] = "<path d='M2 9v6m3-9v12m3-6h8m3-6v12m3-9v6M5 12h3m8 0h3'/>",
        ["band"] = "<path d='M7 5c-3 0-5 3-5 7s2 7 5 7c2 0 3-2 5-2s3 2 5 2c3 0 5-3 5-7s-2-7-5-7c-2 0-3 2-5 2S9 5 7 5Z'/>"
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
