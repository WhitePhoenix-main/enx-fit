using System.ComponentModel.DataAnnotations;
using enx_fit.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace enx_fit.ViewModels;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class ExerciseIdentifierAttribute : ValidationAttribute
{
    public ExerciseIdentifierAttribute() => ErrorMessage = "Выберите упражнение из библиотеки.";
    public override bool IsValid(object? value) => value is Guid id && id != Guid.Empty;
}

// Old bookmarks, forms and drafts continue to resolve to the migrated exercise.
public sealed class ExerciseIdModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext context)
    {
        var value = context.ValueProvider.GetValue(context.ModelName);
        if (value == ValueProviderResult.None) return Task.CompletedTask;
        context.ModelState.SetModelValue(context.ModelName, value);
        if (string.IsNullOrWhiteSpace(value.FirstValue) && context.ModelType == typeof(Guid?))
            context.Result = ModelBindingResult.Success(null);
        else if (ExerciseIds.TryParse(value.FirstValue, out var id)) context.Result = ModelBindingResult.Success(id);
        else context.ModelState.TryAddModelError(context.ModelName, "Неверный идентификатор упражнения.");
        return Task.CompletedTask;
    }
}
