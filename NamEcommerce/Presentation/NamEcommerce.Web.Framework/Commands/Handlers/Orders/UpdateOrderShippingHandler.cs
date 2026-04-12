using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Orders;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Web.Contracts.Commands.Models.Orders;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Orders;

public sealed class UpdateOrderShippingHandler : IRequestHandler<UpdateOrderShippingCommand, CommonResultModel>
{
    private readonly IOrderAppService _orderAppService;

    public UpdateOrderShippingHandler(IOrderAppService orderAppService)
        => _orderAppService = orderAppService;

    public async Task<CommonResultModel> Handle(UpdateOrderShippingCommand request, CancellationToken cancellationToken)
    {
        var result = await _orderAppService.UpdateShippingAsync(new UpdateOrderShippingAppDto
        {
            OrderId = request.OrderId,
            Address = request.Address
        });

        return new CommonResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage
        };
    }
}
