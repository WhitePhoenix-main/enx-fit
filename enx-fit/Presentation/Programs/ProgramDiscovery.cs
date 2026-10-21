using enx_fit.Models;

namespace enx_fit.Presentation.Programs;

public static class ProgramDiscovery
{
    public static bool MatchesSearch(TrainingProgram program, string? search) => string.IsNullOrWhiteSpace(search) ||
        new[] { program.Name, program.Description, program.Goal, program.Categories }
            .Any(value => value?.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase) == true);

    // A time budget must cover every session, not just a misleading average.
    public static bool FitsDuration(TrainingProgram program, int? minutes) => !minutes.HasValue ||
        program.Workouts.Count > 0 && program.Workouts.All(w => w.EstimatedMinutes > 0 && w.EstimatedMinutes <= minutes);

    public static IReadOnlyList<string> Reasons(TrainingProgram program, DashboardSettings? preferences)
    {
        if (preferences?.SetupStatus != SetupStatus.Completed) return [];
        var reasons = new List<string>();
        if (TrainingPreferences.HasEquipment(program, preferences.AvailableEquipment)) reasons.Add("Есть всё оборудование");
        if (preferences.TrainingLevel == program.Level) reasons.Add("Ваш уровень");
        var days = TrainingPreferences.SelectedDays(preferences.PreferredDays).Length;
        if (days > 0 && days == program.DaysPerWeek) reasons.Add("Под ваш ритм");
        if (TrainingPreferences.MatchesGoal(program, preferences.TrainingGoal)) reasons.Add("Совпадает с целью");
        return reasons;
    }
}
