using MediatR;
using NamEcommerce.Web.Contracts.Queries.Models.Orders;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Orders;

public sealed class GetOrderPrepaidAmountHandler(IMediator mediator) : IRequestHandler<GetOrderPrepaidAmountQuery, decimal>
{
    public async Task<decimal> Handle(GetOrderPrepaidAmountQuery request, CancellationToken cancellationToken)
    {
        var paidAmount = await mediator.Send(new GetOrderPaidAmountQuery { OrderId = request.OrderId }).ConfigureAwait(false);
        var debtAmount = await mediator.Send(new GetOrderDebtAmountQuery { OrderId = request.OrderId }).ConfigureAwait(false);

        return paidAmount - debtAmount;
    }
}
