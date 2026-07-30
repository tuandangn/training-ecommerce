using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Orders;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Web.Contracts.Commands.Models.Orders;
using NamEcommerce.Web.Contracts.Models.FastSales;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Orders;

public sealed class QuickCreateOrderHandler(IQuickCreateOrderAppService fastSaleAppService, IOrderAppService orderAppService)
    : IRequestHandler<QuickCreateOrderCommand, QuickCreateOrderResultModel>
{
    public async Task<QuickCreateOrderResultModel> Handle(QuickCreateOrderCommand request, CancellationToken cancellationToken)
    {
        var result = await fastSaleAppService.QuickCreateOrderAsync(new QuickCreateOrderAppDto
        {
            CustomerId = request.CustomerId,
            Items = request.Items.Select(item => new QuickCreateOrderAppDto.QuickCreateOrderItemAppDto2
            {
                ProductId = item.ProductId,
                WarehouseId = item.WarehouseId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            }).ToList(),
            OrderDiscount = request.OrderDiscount,
            Note = request.Note,
            DeliveryNow = request.DeliveryNow,
            ShippingAddress = request.ShippingAddress,
            ShippingPhoneNumber = request.ShippingPhoneNumber
        }).ConfigureAwait(false);

        if (!result.Success)
        {
            return new QuickCreateOrderResultModel
            {
                Success = false,
                ErrorMessage = result.ErrorMessage
            };
        }

        var createdOrder = await orderAppService.GetOrderByIdAsync(result.OrderId!.Value).ConfigureAwait(false);
        return new QuickCreateOrderResultModel
        {
            Success = true,
            OrderDiscount = createdOrder!.OrderDiscount ?? 0,
            OrderSubTotal = createdOrder.OrderSubTotal,
            OrderTotal = createdOrder.TotalAmount,
            OrderId = createdOrder.Id,
            OrderCode = createdOrder.Code
        };
    }
}
