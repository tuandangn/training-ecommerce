using MediatR;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Application.Contracts.Dtos.Orders;
using NamEcommerce.Web.Contracts.Commands.Models.Orders;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Orders;

public sealed class CancelOrderHandler : IRequestHandler<CancelOrderCommand, CancelOrderResultModel>
{
    private readonly IOrderAppService _orderAppService;

    public CancelOrderHandler(IOrderAppService orderAppService) 
        => _orderAppService = orderAppService;

    public async Task<CancelOrderResultModel> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var result = await _orderAppService.CancelOrderAsync(new CancelOrderAppDto
        {
            OrderId = request.OrderId,
            Reason = request.Reason
        });

        return new CancelOrderResultModel(result.Success, result.ErrorMessage);
    }
}
