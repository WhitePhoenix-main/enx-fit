using System.ComponentModel.DataAnnotations;

namespace enx_fit.ViewModels.Dashboard;

public sealed class GoalInput
{
    [Required(ErrorMessage = "Напишите цель."), StringLength(120, ErrorMessage = "Не больше 120 символов.")]
    public string Title { get; set; } = "Тренироваться регулярно";
    [Range(1, 14, ErrorMessage = "Укажите от 1 до 14 тренировок в неделю.")]
    public int WeeklyWorkouts { get; set; } = 4;
    [StringLength(1000, ErrorMessage = "Не больше 1000 символов.")] public string? TrainerNote { get; set; }
}
