using System.ComponentModel.DataAnnotations;

namespace enx_fit.ViewModels.Dashboard;

public sealed class CheckInInput
{
    [Range(0, 15000, ErrorMessage = "Вода: от 0 до 15 000 мл.")] public int WaterMl { get; set; }
    [Range(0, 100000, ErrorMessage = "Шаги: от 0 до 100 000.")] public int Steps { get; set; }
    [Range(0, 1440, ErrorMessage = "Сон: от 0 до 1 440 минут.")] public int SleepMinutes { get; set; }
    [Range(0, 1440, ErrorMessage = "Чтение: от 0 до 1 440 минут.")] public int ReadingMinutes { get; set; }
    public bool NutritionLogged { get; set; }
    public bool StretchingDone { get; set; }
}
