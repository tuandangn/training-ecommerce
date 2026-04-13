using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Orders;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Web.Contracts.Commands.Models.Orders;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Orders;

public sealed class UpdateOrderDiscountHandler : IRequestHandler<UpdateOrderDiscountCommand, CommonResultModel>
{
    private readonly IOrderAppService _orderAppService;

    public UpdateOrderDiscountHandler(IOrderAppService orderAppService)
        => _orderAppService = orderAppService;

    public async Task<CommonResultModel> Handle(UpdateOrderDiscountCommand request, CancellationToken cancellationToken)
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

        if((request.Discount ?? 0) > order.OrderSubTotal)
        {
            return new CommonResultModel
            {
                Success = false,
                ErrorMessage = "Giảm giá không được lớn hơn tổng đơn."
            };
        }

        var result = await _orderAppService.UpdateOrderAsync(new UpdateOrderAppDto(order.Id)
        {
            ExpectedShippingDateUtc = order.ExpectedShippingDateUtc,
            OrderDiscount = request.Discount,
            Note = order.Note
        }).ConfigureAwait(false);

        return new CommonResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage
        };
    }
}
