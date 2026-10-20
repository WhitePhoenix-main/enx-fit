using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.WebUtilities;

namespace enx_fit.TagHelpers;

// Native forms and AJAX submissions retain the explicitly authorized coach context.
[HtmlTargetElement("form")]
public sealed class CoachWorkoutFormTagHelper : TagHelper
{
    [ViewContext, HtmlAttributeNotBound] public ViewContext ViewContext { get; set; } = null!;
    public override int Order => 10000;
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (ViewContext.ViewData["CoachWorkout"] is not true) return;
        var action = output.Attributes["action"]?.Value?.ToString();
        if (action is not null && action.StartsWith("/Workouts/Details/", StringComparison.OrdinalIgnoreCase))
            output.Attributes.SetAttribute("action", QueryHelpers.AddQueryString(action, "Coach", "true"));
    }
}
