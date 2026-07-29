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
            CustomerName = order.CustomerInfo.FullName,
            CustomerPhone = order.CustomerInfo.PhoneNumber,
            CustomerAddress = order.CustomerInfo.Address,
            IsRetailWalkInCustomer = order.CustomerInfo.IsRetailWalkInCustomer,
            CreatedByUserId = order.CreatedByUserId,
            CreatedByUsername = order.CreatedByUsername,
            Note = order.Note,
            CompletedOnUtc = order.CompletedOnUtc,
            ExpectedShippingDateUtc = order.ExpectedShippingDateUtc,
            ShippingAddress = order.ShippingAddress,
            ShippingPhoneNumber = order.ShippingPhoneNumber,
            CreatedOnUtc = order.CreatedOnUtc,
            CanUpdateOrderItems = order.CanUpdateOrderItems(),
            CanUpdateInfo = order.CanUpdateInfo(),
            CanCompleteOrder = order.CanCompleteOrder(),
            CanProcess = order.CanProcess(),
            ProcessRequiresPayment = order.ProcessRequiresPayment,
            PaidAmount = order.PaidAmount ?? 0,
            HasPayments = order.HasPayments(),
            HadPaid = order.HadPaid()
        };

        foreach (var orderItem in order.OrderItems)
        {
            dto.Items.Add(new OrderItemDto(orderItem.Id)
            {
                OrderId = order.Id,
                ProductId = orderItem.ProductId,
                ProductName = orderItem.ProductName,
                Quantity = orderItem.Quantity,
                UnitPrice = orderItem.UnitPrice,
                SubTotal = orderItem.SubTotal,
                IsDelivered = orderItem.IsDelivered,
                DeliveredOnUtc = orderItem.DeliveredOnUtc
            });
        }

        return dto;
    }
}
