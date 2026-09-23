using System.ComponentModel.DataAnnotations;
using enx_fit.Models;

namespace enx_fit.ViewModels;

public sealed class ProgramInput : IValidatableObject
{
    public Guid Revision { get; set; }
    [Required(ErrorMessage = "Укажите название программы."), StringLength(160)] public string Name { get; set; } = "";
    [Required(ErrorMessage = "Укажите цель."), StringLength(120)] public string Goal { get; set; } = "";
    [StringLength(2000)] public string? Description { get; set; }
    [EnumDataType(typeof(ProgramLevel))] public ProgramLevel Level { get; set; }
    [Range(1, 52)] public int Weeks { get; set; } = 8;
    [Range(1, 7)] public int DaysPerWeek { get; set; } = 3;
    public List<ProgramWorkoutInput> Workouts { get; set; } = [];
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (Workouts.Count > 14) yield return new("Допускается до 14 тренировок в программе.");
        var days = Workouts.Where(w => w.DayOfWeek.HasValue).Select(w => w.DayOfWeek).ToList();
        if (days.Distinct().Count() != days.Count) yield return new("На один день можно назначить одну тренировку.");
        if (days.Count > 0 && days.Count != DaysPerWeek) yield return new("Количество дней в расписании должно совпадать с частотой тренировок.");
        var keys = Workouts.Where(w => w.Key != Guid.Empty).Select(w => w.Key).ToList();
        if (keys.Distinct().Count() != keys.Count) yield return new("Повторяющаяся тренировка. Обновите страницу.");
    }
    public static ProgramInput From(TrainingProgram p) => new()
    {
        Revision = p.Revision, Name = p.Name, Goal = p.Goal, Description = p.Description, Level = p.Level,
        Weeks = p.Weeks, DaysPerWeek = p.DaysPerWeek,
        Workouts = p.Workouts.OrderBy(w => w.Order).Select(w => new ProgramWorkoutInput
        {
            Key = w.Key, Name = w.Name, DayOfWeek = w.DayOfWeek, EstimatedMinutes = w.EstimatedMinutes,
            Exercises = w.Exercises.OrderBy(e => e.Order).Select(e => new ProgramExerciseInput
            { ExerciseId = e.ExerciseId, Prescription = e.Prescription }).ToList()
        }).ToList()
    };
}

public sealed class ProgramWorkoutInput : IValidatableObject
{
    public Guid Key { get; set; }
    [Required, StringLength(120)] public string Name { get; set; } = "Новая тренировка";
    [Range(1, 7)] public int? DayOfWeek { get; set; }
    [Range(5, 300)] public int EstimatedMinutes { get; set; } = 60;
    public List<ProgramExerciseInput> Exercises { get; set; } = [];
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (Exercises.Count > 30) yield return new("Допускается до 30 упражнений в тренировке.");
        if (Exercises.Select(e => e.ExerciseId).Distinct().Count() != Exercises.Count)
            yield return new("Упражнение уже добавлено в эту тренировку.");
    }
}

public sealed class ProgramExerciseInput : IValidatableObject
{
    [Range(1, int.MaxValue)] public int ExerciseId { get; set; }
    public ExercisePrescription Prescription { get; set; } = new();
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (Prescription.RepsMin > Prescription.RepsMax) yield return new("Минимум повторений не может быть больше максимума.");
        if (Prescription.WeightKg.HasValue && Prescription.PercentOneRepMax.HasValue)
            yield return new("Укажите вес или процент 1ПМ, а не оба значения.");
    }
}

public sealed class AssignmentInput
{
    [Required(ErrorMessage = "Выберите клиента.")] public string ClientId { get; set; } = "";
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public List<int> Days { get; set; } = [];
}
