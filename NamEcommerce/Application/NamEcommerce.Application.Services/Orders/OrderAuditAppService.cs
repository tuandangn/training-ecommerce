using NamEcommerce.Application.Contracts.Dtos.Orders;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Domain.Shared.Services.Orders;

namespace NamEcommerce.Application.Services.Orders;

public sealed class OrderAuditAppService(
    IOrderItemChangeAuditManager orderItemChangeAuditManager) : IOrderAuditAppService
{
    public async Task<IReadOnlyList<OrderItemChangeAuditAppDto>> GetOrderItemChangeAuditsAsync(Guid orderId)
    {
        var audits = await orderItemChangeAuditManager.GetByOrderIdAsync(orderId).ConfigureAwait(false);

        return audits.Select(audit => new OrderItemChangeAuditAppDto(audit.Id)
        {
            OrderId = audit.OrderId,
            OrderItemId = audit.OrderItemId,
            ProductId = audit.ProductId,
            ProductName = audit.ProductName,
            Action = (int)audit.Action,
            OldQuantity = audit.OldQuantity,
            NewQuantity = audit.NewQuantity,
            OldUnitPrice = audit.OldUnitPrice,
            NewUnitPrice = audit.NewUnitPrice,
            ChangedByUserId = audit.ChangedByUserId,
            ChangedByUsername = audit.ChangedByUsername,
            CreatedOnUtc = audit.CreatedOnUtc
        }).ToList();
    }
}
