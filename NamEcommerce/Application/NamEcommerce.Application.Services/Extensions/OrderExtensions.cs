using NamEcommerce.Application.Contracts.Dtos.Orders;
using NamEcommerce.Domain.Shared.Dtos.Orders;
using NamEcommerce.Domain.Shared.Enums.Orders;

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
            CustomerName = order.CustomerName,
            CustomerAddress = order.CustomerAddress,
            CustomerPhone = order.CustomerPhone,
            OrderSubTotal = order.OrderSubTotal,
            TotalAmount = order.TotalAmount,
            OrderDiscount = order.OrderDiscount ?? 0,
            Status = (int)order.Status,
            IsFinished = order.Status == OrderStatus.Locked,
            Note = order.Note,
            ShippingAddress = order.ShippingAddress,
            LockOrderReason = order.LockOrderReason,
            CreatedByUserId = order.CreatedByUserId,
            CreatedByUsername = order.CreatedByUsername,
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
                ProductName = orderItem.ProductName,
                Quantity = orderItem.Quantity,
                UnitPrice = orderItem.UnitPrice,
                IsDelivered = orderItem.IsDelivered,
                DeliveredOnUtc = orderItem.DeliveredOnUtc
            });
        }

        return dto;
    }
}
