using MediatR;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Web.Contracts.Queries.Models.Orders;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Orders;

public sealed class GetOrderDebtAmountHandler(ICustomerDebtAppService customerDebtAppService)
    : IRequestHandler<GetOrderDebtAmountQuery, decimal>
{
    public Task<decimal> Handle(GetOrderDebtAmountQuery request, CancellationToken cancellationToken)
        => customerDebtAppService.GetTotalDebtByOrderAsync(request.OrderId);
}
