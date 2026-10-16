using enx_fit.Pages.Dashboard;
namespace enx_fit.ViewModels.Dashboard;

public sealed record DashboardWidgetViewModel(enx_fit.Pages.Dashboard.DashboardModel Dashboard, string WidgetId)
{
    public string? PartialName
    {
        get
        {
            var name = Dashboard.CurrentPage == "/Dashboard/Index" ? WidgetId switch
            {
                "welcome" => "Home/_Welcome.cshtml",
                "next" => "Home/_Next.cshtml",
                "goal" => "Home/_WeekPlan.cshtml",
                "recommendations" => "Home/_Recommendation.cshtml",
                "workouts" => "Home/_LastWorkout.cshtml",
                "strength" => "Home/_Strength.cshtml",
                "stat-weight" => "Home/_Measurements.cshtml",
                "habits" => "Home/_Habits.cshtml",
                "stat-workouts" or "stat-goal" or "stat-volume" => "Home/_Stats.cshtml",
                _ => SectionPartialName
            } : SectionPartialName;
            return name is null ? null : "/Pages/Shared/Dashboard/Widgets/" + name;
        }
    }

    private string? SectionPartialName => WidgetId switch
    {
        "welcome" => "Section/_Welcome.cshtml",
        "weight" => "Section/_Weight.cshtml",
        "records" => "Section/_Records.cshtml",
        "goal" => "Section/_Goal.cshtml",
        "volume" => "Section/_Volume.cshtml",
        "workouts" => "Section/_Workouts.cshtml",
        "recommendations" => "Section/_Recommendations.cshtml",
        "strength" => "Section/_Strength.cshtml",
        "today" => "Section/_Today.cshtml",
        "next" => "Section/_Next.cshtml",
        "sleep" => "Section/_Sleep.cshtml",
        "habits" => "Section/_Habits.cshtml",
        "achievements" => "Section/_Achievements.cshtml",
        "calendar" => "Section/_Calendar.cshtml",
        "stat-volume" or "stat-workouts" or "stat-weight" or "stat-streak" or "stat-goal" => "Section/_Stats.cshtml",
        _ => null
    };
}
