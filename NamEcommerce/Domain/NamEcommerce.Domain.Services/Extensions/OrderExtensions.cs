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
            TotalAmount = order.OrderTotal,
            OrderDiscount = order.OrderDiscount, // Important to map discount too
            Status = order.OrderStatus,
            CustomerId = order.CustomerId,
            CreatedByUserId = order.CreatedByUserId,
            Note = order.Note,
            PaymentStatus = order.PaymentStatus,
            PaymentMethod = order.PaymentMethod,
            PaidOnUtc = order.PaidOnUtc,
            PaymentNote = order.PaymentNote,
            ShippingStatus = order.ShippingStatus,
            ShippingAddress = order.ShippingAddress,
            ShippedOnUtc = order.ShippedOnUtc,
            ShippingNote = order.ShippingNote,
            CancellationReason = order.CancellationReason,
            ExpectedShippingDateUtc = order.ExpectedShippingDateUtc
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
