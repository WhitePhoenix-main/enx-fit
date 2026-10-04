using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace enx_fit.ModelBinding;

public sealed class BodyDecimalModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext context)
    {
        var value = context.ValueProvider.GetValue(context.ModelName);
        if (value == ValueProviderResult.None) return Task.CompletedTask;
        context.ModelState.SetModelValue(context.ModelName, value);
        var text = value.FirstValue?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            if (context.ModelMetadata.IsNullableValueType) context.Result = ModelBindingResult.Success(null);
            else context.ModelState.TryAddModelError(context.ModelName, "Укажите значение.");
        }
        else if (decimal.TryParse(text.Replace(',', '.'), NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture, out var number)) context.Result = ModelBindingResult.Success(number);
        else context.ModelState.TryAddModelError(context.ModelName, "Введите число, например 80,5.");
        return Task.CompletedTask;
    }
}
