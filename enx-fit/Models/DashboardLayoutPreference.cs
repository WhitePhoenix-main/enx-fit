namespace enx_fit.Models;

public sealed class DashboardLayoutPreference
{
    public string UserId { get; set; } = "";
    public string Mode { get; set; } = "personal";
    public string WidgetsJson { get; set; } = "[]";
}
