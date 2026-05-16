using NamEcommerce.Application.Contracts.Dtos.Dashboard;

namespace NamEcommerce.Application.Contracts.Dashboard;

public interface IDashboardAppService
{
    Task<DashboardAppDto> GetDashboardDataAsync();
}
