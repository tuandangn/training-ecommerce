using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Shared.Dtos.Orders;

namespace NamEcommerce.Domain.Services.Extensions;

public static class OrderExtensions
{
    public static OrderDto ToDto(this Order order)
    {
        var dto = new OrderDto(order.Id)
        {
            Code = order.Code,
            OrderSubTotal = order.OrderSubTotal,
            TotalAmount = order.OrderTotal,
            OrderDiscount = order.OrderDiscount,
            Status = order.OrderStatus,
            CustomerId = order.CustomerId,
            CreatedByUserId = order.CreatedByUserId,
            Note = order.Note,
            LockOrderReason = order.LockOrderReason,
            ExpectedShippingDateUtc = order.ExpectedShippingDateUtc,
            ShippingAddress = order.ShippingAddress,
            CreatedOnUtc = order.CreatedOnUtc,
            CanUpdateOrderItems = order.CanUpdateOrderItems(),
            CanUpdateInfo = order.CanUpdateInfo(),
            CanLockOrder = order.CanLockOrder()
        };

        foreach (var orderItem in order.OrderItems)
        {
            dto.Items.Add(new OrderItemDto(orderItem.Id)
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
