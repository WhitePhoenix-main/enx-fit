namespace enx_fit.Models;

public sealed class DashboardSettings
{
    public string UserId { get; set; } = "";
    public string GoalTitle { get; set; } = "Тренироваться регулярно";
    public int WeeklyWorkoutGoal { get; set; } = 4;
    public string? TrainerNote { get; set; }
    public SetupStatus SetupStatus { get; set; }
    public TrainingGoal? TrainingGoal { get; set; }
    public TrainingLocation? TrainingLocation { get; set; }
    public TrainingEquipment AvailableEquipment { get; set; }
    public int PreferredDays { get; set; }
    public Guid PreferencesRevision { get; set; }
}
