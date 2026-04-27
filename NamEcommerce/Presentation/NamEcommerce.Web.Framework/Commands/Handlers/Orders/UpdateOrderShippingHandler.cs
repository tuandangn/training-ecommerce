using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Orders;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Web.Contracts.Commands.Models.Orders;
using NamEcommerce.Web.Contracts.Extensions;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Framework.Services;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Orders;

public sealed class UpdateOrderShippingHandler : IRequestHandler<UpdateOrderShippingCommand, CommonActionResultModel>
{
    private readonly IOrderAppService _orderAppService;

    public UpdateOrderShippingHandler(IOrderAppService orderAppService)
        => _orderAppService = orderAppService;

    public async Task<CommonActionResultModel> Handle(UpdateOrderShippingCommand request, CancellationToken cancellationToken)
    {
        var result = await _orderAppService.UpdateShippingAsync(new UpdateOrderShippingAppDto
        {
            OrderId = request.OrderId,
            ExpectedShippingDateUtc = DateTimeHelper.ToUniversalTime(request.ExpectedShippingDate.ToEndOfDate()),
            Address = request.Address
        });

        return new CommonActionResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage
        };
    }
}
