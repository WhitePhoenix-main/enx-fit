using System.ComponentModel.DataAnnotations;

namespace enx_fit.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class ExerciseIdentifierAttribute : ValidationAttribute
{
    public ExerciseIdentifierAttribute() => ErrorMessage = "Выберите упражнение из библиотеки.";
    public override bool IsValid(object? value) => value is Guid id && id != Guid.Empty;
}
