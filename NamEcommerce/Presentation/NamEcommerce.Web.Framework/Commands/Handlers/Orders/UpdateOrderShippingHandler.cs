using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Orders;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Web.Contracts.Commands.Models.Orders;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Orders;

public sealed class UpdateOrderShippingHandler : IRequestHandler<UpdateOrderShippingCommand, UpdateOrderShippingResultModel>
{
    private readonly IOrderAppService _orderAppService;

    public UpdateOrderShippingHandler(IOrderAppService orderAppService)
        => _orderAppService = orderAppService;

    public async Task<UpdateOrderShippingResultModel> Handle(UpdateOrderShippingCommand request, CancellationToken cancellationToken)
    {
        var result = await _orderAppService.UpdateShippingAsync(new UpdateOrderShippingAppDto
        {
            OrderId = request.OrderId,
            ShippingStatus = request.ShippingStatus,
            Address = request.Address,
            Note = request.Note
        });

        return new UpdateOrderShippingResultModel(result.Success, result.ErrorMessage);
    }
}
