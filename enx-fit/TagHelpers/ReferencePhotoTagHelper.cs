using Microsoft.AspNetCore.Razor.TagHelpers;

namespace enx_fit.TagHelpers;

// Only the photograph window is rendered; reference labels and controls stay in HTML.
[HtmlTargetElement("reference-photo")]
public sealed class ReferencePhotoTagHelper : TagHelper
{
    public string Image { get; set; } = "workouts";
    public string Window { get; set; } = "0 0 100 100";
    public string Alt { get; set; } = "";

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var (width, height) = Image == "overview" ? (1632, 963) : (948, 1659);
        output.TagName = "svg";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("viewBox", Window);
        output.Attributes.SetAttribute("preserveAspectRatio", "xMidYMid slice");
        if (Alt.Length == 0) output.Attributes.SetAttribute("aria-hidden", "true");
        else
        {
            output.Attributes.SetAttribute("role", "img");
            output.Attributes.SetAttribute("aria-label", Alt);
        }
        output.Content.SetHtmlContent($"<image href='/images/mobile/{Image}.png' width='{width}' height='{height}' />");
    }
}
