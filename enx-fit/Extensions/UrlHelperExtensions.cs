using Microsoft.AspNetCore.Mvc;

namespace enx_fit.Extensions;

public static class UrlHelperExtensions
{
    private const string HandlerPrefix = "On";
    private const string AsyncSuffix = "Async";

    private static readonly string[] HttpMethods =
    [
        "Get",
        "Post",
        "Put",
        "Delete",
        "Patch",
        "Head",
        "Options"
    ];

    public static string PageUrl(
        this IUrlHelper url,
        string pageName,
        string? handlerMethodName = null,
        object? routeValues = null)
    {
        ArgumentNullException.ThrowIfNull(url);
        ArgumentException.ThrowIfNullOrWhiteSpace(pageName);

        var handler = ToPageHandler(handlerMethodName);

        return url.Page(pageName, handler, routeValues)
            ?? throw new InvalidOperationException(
                $"Unable to generate URL for page '{pageName}' with handler '{handler ?? "<default>"}'.");
    }

    private static string? ToPageHandler(string? methodName)
    {
        if (string.IsNullOrWhiteSpace(methodName))
            return null;

        var name = methodName.Trim();

        // Уже готовое имя handler: "AddExercise".
        if (!name.StartsWith(HandlerPrefix, StringComparison.Ordinal))
            return name;

        if (name.EndsWith(AsyncSuffix, StringComparison.Ordinal))
            name = name.Remove(name.Length - AsyncSuffix.Length);

        if (name.Length == HandlerPrefix.Length)
            throw InvalidHandler(methodName);

        name = name.Remove(0, HandlerPrefix.Length);

        foreach (var method in HttpMethods)
        {
            if (name.Equals(method, StringComparison.Ordinal))
                return null;

            if (name.StartsWith(method, StringComparison.Ordinal)
                && name.Length > method.Length
                && char.IsUpper(name[method.Length]))
            {
                return name.Substring(method.Length);
            }
        }

        throw InvalidHandler(methodName);
    }

    private static ArgumentException InvalidHandler(string methodName) =>
        new(
            $"'{methodName}' is not a valid Razor Page handler method.",
            nameof(methodName));
}
