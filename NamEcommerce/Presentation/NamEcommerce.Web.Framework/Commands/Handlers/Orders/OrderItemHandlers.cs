using MediatR;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Application.Contracts.Dtos.Orders;
using NamEcommerce.Web.Contracts.Commands.Models.Orders;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Orders;

public sealed class UpdateOrderItemHandler : IRequestHandler<UpdateOrderItemCommand, CommonActionResultModel>
{
    private readonly IOrderAppService _orderAppService;

    public UpdateOrderItemHandler(IOrderAppService orderAppService)
    {
        _orderAppService = orderAppService;
    }

    public async Task<CommonActionResultModel> Handle(UpdateOrderItemCommand request, CancellationToken cancellationToken)
    {
        var result = await _orderAppService.UpdateOrderItemAsync(new UpdateOrderItemAppDto
        {
            OrderId = request.OrderId,
            OrderItemId = request.ItemId,
            Quantity = request.Quantity,
            UnitPrice = request.UnitPrice
        }).ConfigureAwait(false);

        return new CommonActionResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage
        };
    }
}
