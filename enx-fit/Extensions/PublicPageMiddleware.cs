namespace enx_fit.Extensions;

public static class PublicPageMiddleware
{
    public static IApplicationBuilder UsePublicPageErrors(this IApplicationBuilder app) =>
        app.UseWhen(context => HttpMethods.IsGet(context.Request.Method) &&
            context.Request.Headers.Accept.Any(value => value?.Contains("text/html", StringComparison.OrdinalIgnoreCase) == true),
            branch => branch.UseStatusCodePagesWithReExecute("/404_page"));
}
