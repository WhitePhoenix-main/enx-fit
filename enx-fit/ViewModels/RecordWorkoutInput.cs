using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using enx_fit.Models;
using Microsoft.AspNetCore.Mvc;

namespace enx_fit.ViewModels;

public sealed class RecordWorkoutInput
{
    [Required, StringLength(160)] public string Title { get; set; } = "Прошедшая тренировка";
    [Required] public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [Range(1, 1440)] public int? DurationMinutes { get; set; }
    [StringLength(1000)] public string? Notes { get; set; }
    [Range(-840, 840)] public int UtcOffsetMinutes { get; set; }
    public Guid ClientRequestId { get; set; } = Guid.NewGuid();
    [Required, StringLength(200000)] public string ResultsJson { get; set; } = "[]";
}

public sealed class RecordedExercise
{
    // Guid.Empty means no exercise was selected.
    [ExerciseIdentifier]
    [JsonConverter(typeof(ExerciseIdJsonConverter))]
    [ModelBinder(BinderType = typeof(ExerciseIdModelBinder))]
    public Guid ExerciseId { get; set; }
    [Required(ErrorMessage = "Укажите подходы."), MinLength(1, ErrorMessage = "Укажите хотя бы один подход."), MaxLength(20, ErrorMessage = "В упражнении может быть не более 20 подходов.")]
    public List<RecordedSet> Sets { get; set; } = [];
}
public sealed class RecordedSet
{
    [Range(typeof(decimal), "0", "10000", ErrorMessage = "Укажите вес от 0 до 10000 кг.")] public decimal Weight { get; set; }
    [Range(1, 1000, ErrorMessage = "Укажите повторения от 1 до 1000.")] public int Reps { get; set; } = 10;
    [Range(0, 10, ErrorMessage = "Укажите RIR от 0 до 10 или оставьте поле пустым.")] public int? Rir { get; set; }
    public bool IsWarmup { get; set; }
    [StringLength(500, ErrorMessage = "Заметка к подходу должна быть не длиннее 500 символов.")] public string? Notes { get; set; }
}
