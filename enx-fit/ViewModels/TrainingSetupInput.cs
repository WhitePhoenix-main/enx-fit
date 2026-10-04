using System.ComponentModel.DataAnnotations;
using enx_fit.Models;

namespace enx_fit.ViewModels;

public sealed class TrainingSetupInput : IValidatableObject
{
    [Required(ErrorMessage = "Выберите цель."), EnumDataType(typeof(TrainingGoal), ErrorMessage = "Выберите цель из списка.")]
    public TrainingGoal? Goal { get; set; }
    [Required(ErrorMessage = "Выберите место занятий."), EnumDataType(typeof(TrainingLocation), ErrorMessage = "Выберите место из списка.")]
    public TrainingLocation? Location { get; set; }
    public List<TrainingEquipment> Equipment { get; set; } = [];
    public List<int> Days { get; set; } = [];
    public bool DaysLater { get; set; }
    public Guid Revision { get; set; }
    public TrainingEquipment EquipmentMask => Equipment.Aggregate(TrainingEquipment.None, (mask, item) => mask | item);
    public int DaysMask => DaysLater ? 0 : Days.Distinct().Aggregate(0, (mask, day) => mask | (1 << (day - 1)));
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Equipment.Count > 7 || Equipment.Any(e => !TrainingPreferences.Equipment.Any(option => option.Value == e)))
            yield return new("Выберите оборудование из списка.", [nameof(Equipment)]);
        if (Days.Count > 7 || Days.Any(day => day is < 1 or > 7))
            yield return new("Выберите дни недели из списка.", [nameof(Days)]);
        else if (!DaysLater && Days.Count == 0)
            yield return new("Выберите хотя бы один день или «Дни выберу позже».", [nameof(Days)]);
    }
}
