using enx_fit.Pages;
using enx_fit.Services;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace enx_fit.ViewModels;

// Constructed only after the page has resolved an authorized client.
public sealed record CoachContextViewModel(string Id, string Name, IReadOnlyList<ClientSummary> Clients, bool CanManage = true)
{
    public static CoachContextViewModel? Resolve(ViewDataDictionary viewData)
    {
        if (viewData["Dashboard"] is DashboardModel { IsCoachView: true, Reference: false } dashboard)
            return new(dashboard.Data.Subject.Id, dashboard.Data.Name, dashboard.Clients, dashboard.CanManageClients);
        return viewData["CoachContext"] as CoachContextViewModel;
    }
}
