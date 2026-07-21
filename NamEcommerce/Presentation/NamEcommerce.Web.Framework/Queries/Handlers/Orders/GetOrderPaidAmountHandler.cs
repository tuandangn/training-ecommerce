using MediatR;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Web.Contracts.Queries.Models.Orders;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Orders;

public sealed class GetOrderPaidAmountHandler(ICustomerDebtAppService customerDebtAppService)
    : IRequestHandler<GetOrderPaidAmountQuery, decimal>
{
    public Task<decimal> Handle(GetOrderPaidAmountQuery request, CancellationToken cancellationToken)
        => customerDebtAppService.GetTotalPaidByOrderAsync(request.OrderId);
}
