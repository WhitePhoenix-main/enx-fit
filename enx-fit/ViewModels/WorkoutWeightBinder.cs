using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace enx_fit.ViewModels;

// HTML number inputs submit a dot regardless of the server's Russian locale.
public sealed class WorkoutWeightBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext context)
    {
        var value = context.ValueProvider.GetValue(context.ModelName);
        if (value == ValueProviderResult.None) return Task.CompletedTask;
        context.ModelState.SetModelValue(context.ModelName, value);
        if (decimal.TryParse(value.FirstValue?.Replace(',', '.'),
                NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var weight))
            context.Result = ModelBindingResult.Success(weight);
        else
            context.ModelState.TryAddModelError(context.ModelName, "Введите корректный вес, например 62,5.");
        return Task.CompletedTask;
    }
}
