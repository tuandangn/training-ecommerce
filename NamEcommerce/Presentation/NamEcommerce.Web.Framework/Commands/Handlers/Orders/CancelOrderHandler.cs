using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Orders;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Web.Contracts.Commands.Models.Orders;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Orders;

public sealed class CancelOrderHandler : IRequestHandler<CancelOrderCommand, CommonResultModel>
{
    private readonly IOrderAppService _orderAppService;

    public CancelOrderHandler(IOrderAppService orderAppService) 
        => _orderAppService = orderAppService;

    public async Task<CommonResultModel> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var result = await _orderAppService.CancelOrderAsync(new CancelOrderAppDto
        {
            OrderId = request.OrderId,
            Reason = request.Reason
        }).ConfigureAwait(false);

        return new CommonResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage
        };
    }
}
