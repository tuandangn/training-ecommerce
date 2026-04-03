using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Shared.Dtos.Orders;

namespace NamEcommerce.Domain.Services.Extensions;

public static class OrderExtensions
{
    public static OrderDto ToDto(this Order order)
    {
        var dto = new OrderDto(order.Id)
        {
            TotalAmount = order.OrderTotal,
            Status = (int)order.OrderStatus,
            CustomerId = order.CustomerId,
            Note = order.Note
        };

        foreach (var i in order.OrderItems)
            dto.Items.Add(new OrderItemDto(i.Id, i.ProductId, i.Quantity, i.UnitPrice));

        return dto;
    }
}
