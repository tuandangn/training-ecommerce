using NamEcommerce.Web.Contracts.Models.Dashboard;

namespace NamEcommerce.Web.Services.Dashboard;

public interface IDashboardModelFactory
{
    Task<DashboardModel> PrepareDashboardModelAsync();
}
