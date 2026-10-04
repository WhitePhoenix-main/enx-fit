using enx_fit.ViewModels;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace enx_fit.Pages.Body;

internal static class BodyFormValidation
{
    public static void Validate(BodyMeasurementInputModel input, ModelStateDictionary state, DateOnly today)
    {
        if (input.Date == default || state.GetValueOrDefault("Input.Date")?.Errors.Count > 0)
        {
            if (state.TryGetValue("Input.Date", out var entry)) entry.Errors.Clear();
            state.AddModelError("Input.Date", "Укажите корректную дату замера.");
        }
        else if (input.Date > today) state.TryAddModelError("Input.Date", "Дата замера не может быть в будущем.");
    }
}
