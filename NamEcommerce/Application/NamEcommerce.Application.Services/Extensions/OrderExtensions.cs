using NamEcommerce.Application.Contracts.Dtos.Orders;
using NamEcommerce.Domain.Shared.Dtos.Orders;

namespace NamEcommerce.Application.Services.Extensions;

public static class OrderExtensions
{
    public static OrderAppDto ToDto(this OrderDto order)
    {
        var dto = new OrderAppDto(order.Id)
        {
            Code = order.Code,
            ExpectedShippingDateUtc = order.ExpectedShippingDateUtc,
            CustomerId = order.CustomerId,
            TotalAmount = order.TotalAmount,
            OrderDiscount = order.OrderDiscount ?? 0,
            Status = (int)order.Status,
            Note = order.Note,
            ShippingAddress = order.ShippingAddress,
            LockOrderReason = order.LockOrderReason,
            CreatedByUserId = order.CreatedByUserId,
            CreatedOnUtc = order.CreatedOnUtc,
            CanUpdateInfo = order.CanUpdateInfo,
            CanUpdateOrderItems = order.CanUpdateOrderItems,
            CanCancelOrder = order.CanLockOrder
        };
        foreach (var orderItem in order.Items)
        {
            dto.Items.Add(new OrderItemAppDto(orderItem.Id)
            {
                OrderId = order.Id,
                ProductId = orderItem.ProductId,
                Quantity = orderItem.Quantity,
                UnitPrice = orderItem.UnitPrice
            });
        }

        return dto;
    }
}
