using Microsoft.AspNetCore.Mvc.Filters;

namespace enx_fit.Diagnostics;

public sealed class RequestPerformanceFilter(RequestPerformance performance) : IAsyncPageFilter, IAsyncResultFilter
{
    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;

    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        using var measurement = performance.Measure("handler");
        await next();
    }

    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        using var measurement = performance.Measure("render");
        await next();
    }
}
