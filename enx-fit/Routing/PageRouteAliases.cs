using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Routing;

// File categories are independent of public URLs. Index aliases accept old bookmarks
// while canonical links continue to use the shorter route declared by each page.
public static class PageRouteAliases
{
    public static void Configure(RazorPagesOptions options)
    {
        AddAlias(options, "/Progress/ExerciseAnalytics/Index", "Analytics/Index");
        AddAlias(options, "/Progress/Measurements/Index", "Body/Index");
        AddAlias(options, "/Training/Exercises/Index", "Exercises/Index");
        AddAlias(options, "/Public/Index", "Index");
        AddAlias(options, "/Training/Workouts/Index", "Workouts/Index");
    }

    private static void AddAlias(RazorPagesOptions options, string page, string template) =>
        options.Conventions.AddPageRouteModelConvention(page, model => model.Selectors.Add(new SelectorModel
        {
            AttributeRouteModel = new AttributeRouteModel
            {
                Template = template,
                SuppressLinkGeneration = true
            }
        }));
}
