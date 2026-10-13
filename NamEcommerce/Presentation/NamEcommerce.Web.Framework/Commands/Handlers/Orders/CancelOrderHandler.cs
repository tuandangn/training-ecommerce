using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Orders;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Web.Contracts.Commands.Models.Orders;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Orders;

public sealed class CancelOrderHandler(IOrderAppService orderAppService) : IRequestHandler<CancelOrderCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var result = await orderAppService.CancelOrderAsync(new CancelOrderAppDto
        {
            OrderId = request.OrderId,
            ReturnWarehouseId = request.ReturnWarehouseId
        }).ConfigureAwait(false);

        return new CommonActionResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage
        };
    }
}
