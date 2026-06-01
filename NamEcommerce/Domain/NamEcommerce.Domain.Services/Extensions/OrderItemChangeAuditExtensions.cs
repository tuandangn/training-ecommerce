using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Shared.Dtos.Orders;

namespace NamEcommerce.Domain.Services.Extensions;

public static class OrderItemChangeAuditExtensions
{
    public static OrderItemChangeAuditDto ToDto(this OrderItemChangeAudit audit)
        => new(audit.Id)
        {
            OrderId = audit.OrderId,
            OrderItemId = audit.OrderItemId,
            ProductId = audit.ProductId,
            ProductName = audit.ProductName,
            Action = audit.Action,
            OldQuantity = audit.OldQuantity,
            NewQuantity = audit.NewQuantity,
            OldUnitPrice = audit.OldUnitPrice,
            NewUnitPrice = audit.NewUnitPrice,
            ChangedByUserId = audit.ChangedByUserId,
            ChangedByUsername = audit.ChangedByUsername,
            CreatedOnUtc = audit.CreatedOnUtc
        };
}
