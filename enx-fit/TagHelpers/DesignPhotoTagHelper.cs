using Microsoft.AspNetCore.Razor.TagHelpers;

namespace enx_fit.TagHelpers;

// SVG viewports reuse only the supplied photography; all UI is accessible HTML.
[HtmlTargetElement("design-photo")]
public sealed class DesignPhotoTagHelper : TagHelper
{
    public string Kind { get; set; } = "row";
    public string Alt { get; set; } = "";
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var (source, window) = Kind switch {
            "bench" => ("current-pro", "272 279 357 202"),
            "squat" => ("template-photos-v1", "512 0 512 512"),
            "pullup" => ("current-pro", "272 753 130 96"),
            "pulldown" => ("training/current-reference", "265 743 130 116"),
            "curl" => ("pro-hero-v1", "925 400 600 620"),
            "row" => ("template-photos-v1", "0 0 512 512"),
            "bench-wide" => ("current-free", "293 159 272 135"),
            "bench-coach" => ("current-coach", "558 349 296 184"),
            "bench-pro" => ("current-pro", "272 279 357 202"),
            "overhead" => ("template-photos-v1", "0 512 512 512"),
            "deadlift" => ("template-photos-v1", "512 512 512 512"),
            "dip" => ("exercises-pro", "1019 641 229 87"),
            "pushup" => ("template-photos-v1", "1024 0 512 512"),
            "lunge" => ("template-photos-v1", "1024 512 512 512"),
            "lateral" => ("exercises-free", "644 719 110 96"),
            "incline" => ("exercises-free", "642 600 110 94"),
            "oats" => ("nutrition-pro", "276 494 143 90"),
            "yogurt" => ("nutrition-pro", "442 494 143 90"),
            "chicken" => ("nutrition-pro", "610 494 143 90"),
            "smoothie" => ("nutrition-pro", "778 494 143 90"),
            "salmon" => ("nutrition-pro", "947 494 133 90"),
            "pancakes" => ("nutrition-pro", "485 775 181 48"),
            "hero" => ("home-free", "697 150 178 264"),
            "hero-pro" => ("pro-hero-v1", "760 0 776 1024"),
            "coach" => ("home-coach", "530 153 131 223"),
            _ => ("template-photos-v1", "0 0 512 512")
        };
        output.TagName = "svg";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("viewBox", window);
        output.Attributes.SetAttribute("preserveAspectRatio", "xMidYMid slice");
        if (Alt.Length == 0) output.Attributes.SetAttribute("aria-hidden", "true");
        else { output.Attributes.SetAttribute("role", "img"); output.Attributes.SetAttribute("aria-label", Alt); }
        output.Content.SetHtmlContent(source is "pro-hero-v1" or "template-photos-v1"
            ? $"<image href='/images/training/{source}.png' width='1536' height='1024' />"
            : $"<image href='/images/{(source.StartsWith("training/")?source:"references/"+source)}.png' width='1672' height='941' />");
    }
}
