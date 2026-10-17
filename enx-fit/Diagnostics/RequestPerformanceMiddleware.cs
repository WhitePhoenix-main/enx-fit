using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Diagnostics;

public sealed class RequestPerformanceMiddleware(RequestDelegate next, ILogger<RequestPerformanceMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, RequestPerformance performance)
    {
        if (context.GetEndpoint()?.Metadata.GetMetadata<PageActionDescriptor>()?.ViewEnginePath != "/Dashboard/Index")
        {
            await next(context);
            return;
        }

        performance.Enabled = true;
        var started = Stopwatch.GetTimestamp();
        context.Response.OnStarting(() =>
        {
            // Header timings stop when response headers are sent; the log includes the completed render.
            context.Response.Headers.Append("Server-Timing", performance.ServerTiming(
                Stopwatch.GetElapsedTime(started).TotalMilliseconds));
            return Task.CompletedTask;
        });
        var completed = false;
        try
        {
            await next(context);
            completed = true;
        }
        finally
        {
            logger.LogInformation(
                "Dashboard {Method} {StatusCode}: {ElapsedMs:F1} ms, {SqlCommands} SQL commands, {SqlMs:F1} ms SQL; {Stages}; request {RequestId}",
                context.Request.Method, completed ? context.Response.StatusCode : StatusCodes.Status500InternalServerError,
                Stopwatch.GetElapsedTime(started).TotalMilliseconds,
                performance.SqlCommands, performance.SqlMilliseconds, performance.Stages, context.TraceIdentifier);
        }
    }
}
