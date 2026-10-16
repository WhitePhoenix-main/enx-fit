using System.ComponentModel.DataAnnotations;
using enx_fit.Presentation.Dashboard;

namespace enx_fit.ViewModels.Dashboard;

public sealed class DashboardLayoutInput
{
    public List<DashboardWidgetPlacement>? Widgets { get; set; }
}
