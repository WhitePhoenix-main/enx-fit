namespace enx_fit.Models;

public sealed class DashboardSettings
{
    public string UserId { get; set; } = "";
    public string GoalTitle { get; set; } = "Тренироваться регулярно";
    public int WeeklyWorkoutGoal { get; set; } = 4;
    public string? TrainerNote { get; set; }
}
