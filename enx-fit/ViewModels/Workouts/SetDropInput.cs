using System.ComponentModel.DataAnnotations;
using enx_fit.ModelBinding;
using enx_fit.Models;
using Microsoft.AspNetCore.Mvc;

namespace enx_fit.ViewModels.Workouts;

public sealed class SetDropInput
{
    [Range(typeof(decimal), "0", "10000", ErrorMessage = "Укажите вес от 0 до 10000 кг.")]
    [ModelBinder(BinderType = typeof(WorkoutWeightBinder))]
    public decimal Weight { get; set; }

    [Range(1, 1000, ErrorMessage = "Укажите повторения от 1 до 1000.")]
    public int Reps { get; set; } = 10;

    public bool IsBodyweight { get; set; }

    public static IEnumerable<ValidationResult> ValidateDrops(List<SetDropInput>? drops)
    {
        if (drops is null) yield break;
        if (drops.Count > 10)
            yield return new ValidationResult("В дропсете может быть не более 10 снижений веса.", ["Drops"]);
        foreach (var drop in drops)
        {
            if (drop is null)
            {
                yield return new ValidationResult("Заполните каждую ступень дропсета.", ["Drops"]);
                continue;
            }
            List<ValidationResult> errors = [];
            Validator.TryValidateObject(drop, new ValidationContext(drop), errors, true);
            foreach (var error in errors) yield return new ValidationResult(error.ErrorMessage, ["Drops"]);
        }
    }

    public static List<SetDrop> ToEntries(List<SetDropInput>? drops) => drops?.Select((d, i) => new SetDrop
        { Order = i, Weight = d.IsBodyweight ? 0 : d.Weight, Reps = d.Reps, IsBodyweight = d.IsBodyweight }).ToList() ?? [];

    public static List<SetDropInput>? FromEntries(IEnumerable<SetDrop> drops)
    {
        var result = drops.OrderBy(d => d.Order).Select(d => new SetDropInput
            { Weight = d.Weight, Reps = d.Reps, IsBodyweight = d.IsBodyweight }).ToList();
        return result.Count == 0 ? null : result;
    }
}
