using MediatR;
using NamEcommerce.Web.Contracts.Models.Dashboard;
using NamEcommerce.Web.Contracts.Queries.Models.Dashboard;

namespace NamEcommerce.Web.Services.Dashboard;

public sealed class DashboardModelFactory : IDashboardModelFactory
{
    private readonly IMediator _mediator;

    public DashboardModelFactory(IMediator mediator)
    {
        _mediator = mediator;
    }

    public Task<DashboardModel> PrepareDashboardModelAsync()
    {
        return _mediator.Send(new GetDashboardQuery());
    }
}
