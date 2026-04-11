using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Orders;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Web.Contracts.Commands.Models.Orders;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Orders;

public sealed class UpdateOrderNoteHandler : IRequestHandler<UpdateOrderNoteCommand, CommonResultModel>
{
    private readonly IOrderAppService _orderAppService;

    public UpdateOrderNoteHandler(IOrderAppService orderAppService)
        => _orderAppService = orderAppService;

    public async Task<CommonResultModel> Handle(UpdateOrderNoteCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderAppService.GetOrderByIdAsync(request.OrderId).ConfigureAwait(false);
        if (order is null)
        {
            return new CommonResultModel
            {
                Success = false,
                ErrorMessage = "Không tìm thấy đơn hàng."
            };
        }
        var result = await _orderAppService.UpdateOrderAsync(new UpdateOrderAppDto(order.Id)
        {
            ExpectedShippingDateUtc = order.ExpectedShippingDateUtc,
            OrderDiscount = order.OrderDiscount,
            Note = request.Note
        }).ConfigureAwait(false);

        return new CommonResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage
        };
    }
}
