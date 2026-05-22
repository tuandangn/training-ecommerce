using MediatR;
using NamEcommerce.Web.Contracts.Models.Dashboard;

namespace NamEcommerce.Web.Contracts.Queries.Models.Dashboard;

public sealed record GetDashboardQuery : IRequest<DashboardModel>;
