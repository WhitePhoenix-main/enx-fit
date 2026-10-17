using Microsoft.AspNetCore.Razor.TagHelpers;

namespace enx_fit.TagHelpers;

// The home screen's icon set is independent of the admin and legacy dashboard themes.
[HtmlTargetElement("reference-icon")]
public sealed class DashboardReferenceIconTagHelper : TagHelper
{
    public string Name { get; set; } = "bars";
    private static readonly IReadOnlyDictionary<string, string> Paths = new Dictionary<string, string>
    {
        ["home"] = "<path d='m2 10 10-9 10 9-2 1v10h-6v-7h-4v7H4V11Z' fill='currentColor' stroke='none'/>",
        ["home-outline"] = "<path d='m2 10 10-9 10 9M4 9v12h5v-7h6v7h5V9'/>",
        ["workout"] = "<path d='M2 12h20' stroke-width='1.5'/><rect x='2' y='8' width='2.5' height='8' rx='.7' fill='currentColor' stroke='none'/><rect x='6' y='4' width='3.5' height='16' rx='1' fill='currentColor' stroke='none'/><rect x='14.5' y='4' width='3.5' height='16' rx='1' fill='currentColor' stroke='none'/><rect x='19.5' y='8' width='2.5' height='8' rx='.7' fill='currentColor' stroke='none'/>",
        ["calendar"] = "<rect x='3' y='5' width='18' height='16' rx='2'/><path d='M7 2v5m10-5v5M3 10h18m-12 5 2 2 4-4'/>",
        ["calendar-filled"] = "<rect x='3' y='5' width='18' height='17' rx='2' fill='currentColor' stroke='none'/><path d='M7 2v5m10-5v5' fill='none'/><path d='M5 10h14m-10 5 2 2 4-4' stroke='#173154' fill='none'/>",
        ["plus-circle"] = "<circle cx='12' cy='12' r='10'/><path d='M12 7v10M7 12h10'/>",
        ["plus"] = "<path d='M12 3v18M3 12h18'/>",
        ["body-scale"] = "<rect x='2' y='2' width='20' height='20' rx='3'/><path d='M8 3v3a4 4 0 0 0 8 0V3M12 3v4'/>",
        ["up-right"] = "<path d='M5 19 19 5M7 5h12v12'/>",
        ["down-right"] = "<path d='m5 5 14 14M7 19h12V7'/>",
        ["pencil"] = "<path d='m15 3 6 6-12 12H3v-6ZM12 6l6 6M3 15l6 6'/>",
        ["bars"] = "<rect x='3' y='15' width='3.2' height='7' rx='1.3' fill='currentColor' stroke='none'/><rect x='10.4' y='9' width='3.2' height='13' rx='1.3' fill='currentColor' stroke='none'/><rect x='17.8' y='2' width='3.2' height='20' rx='1.3' fill='currentColor' stroke='none'/>",
        ["check-circle"] = "<circle cx='12' cy='12' r='10'/><path d='m7 12 3 3 6-7'/>",
        ["user"] = "<circle cx='12' cy='6' r='4'/><path d='M3 22v-3c0-5 18-5 18 0v3Z'/>",
        ["search"] = "<circle cx='10' cy='10' r='7'/><path d='m15 15 6 6'/>",
        ["bell"] = "<path d='M18 8a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9M10 21h4M12 2V1'/>",
        ["play"] = "<path d='m7 3 14 9L7 21Z' fill='currentColor' stroke='none'/>",
        ["shoe"] = "<path d='m6 3-4 7 1 5 15 5h4v-4l-6-4-2-5-4 1-4-5Zm-3 12 5-1m4-5-3 3m7 0-3 3'/>",
        ["target"] = "<path d='M19 10a9 9 0 1 1-7-7'/><path d='M15 9a5 5 0 1 0 2 4M11 13l10-10m-6 1 6-1v6'/>",
        ["more"] = "<circle cx='4' cy='12' r='1.2' fill='currentColor' stroke='none'/><circle cx='12' cy='12' r='1.2' fill='currentColor' stroke='none'/><circle cx='20' cy='12' r='1.2' fill='currentColor' stroke='none'/>",
        ["info"] = "<circle cx='12' cy='12' r='10'/><path d='M12 11v6m0-10v.1'/>",
        ["clock"] = "<circle cx='12' cy='12' r='10'/><path d='M12 5v7l4 3'/>",
        ["trophy"] = "<path d='M7 2h10v8a5 5 0 0 1-10 0Zm0 2H2v4a5 5 0 0 0 5 5m10-9h5v4a5 5 0 0 1-5 5m-5 2v4m-4 3h8l-3-3h-2Z'/>",
        ["scale"] = "<rect x='2' y='2' width='20' height='20' rx='3'/><path d='M7 5c3-2 7-2 10 0l-2 6H9Zm5 0v6M6 15v3m12-3v3'/>",
        ["sliders"] = "<path d='M2 5h7m4 0h9M2 12h13m4 0h3M2 19h3m4 0h13'/><circle cx='11' cy='5' r='2'/><circle cx='17' cy='12' r='2'/><circle cx='7' cy='19' r='2'/>",
        ["star"] = "<path d='m12 2 3 6.4 7 .9-5 5 .9 7-5.9-3.3L6.1 21l.9-6.7-5-5 7-.9Z'/>",
        ["archive"] = "<path d='M3 7h18v14H3Zm-1-5h20v5H2Zm7 9h6'/>",
        ["shield"] = "<path d='m12 2 9 4v7c0 5-9 9-9 9S3 18 3 13V6Zm0 3v14'/>",
        ["globe"] = "<circle cx='12' cy='12' r='10'/><ellipse cx='12' cy='12' rx='4' ry='10'/><path d='M3 8h18M3 16h18'/>",
        ["crown"] = "<path d='m3 6 5 5 4-8 4 8 5-5-2 12H5Zm2 15h14'/><circle cx='3' cy='5' r='1'/><circle cx='12' cy='2' r='1'/><circle cx='21' cy='5' r='1'/>",
        ["download"] = "<path d='M12 2v14m-6-6 6 6 6-6M3 16v6h18v-6'/>",
        ["card"] = "<rect x='2' y='4' width='20' height='16' rx='2'/><path d='M2 9h20M6 15h4'/>",
        ["mail"] = "<rect x='2' y='4' width='20' height='16' rx='2'/><path d='m3 5 9 8 9-8'/>",
        ["lock"] = "<rect x='4' y='10' width='16' height='12' rx='2'/><path d='M7 10V6a5 5 0 0 1 10 0v4M12 15v3'/>"
    };

    public override void Process(TagHelperContext context, TagHelperOutput output) =>
        SvgIconRenderer.Render(output, Paths, Name, "bars");
}
