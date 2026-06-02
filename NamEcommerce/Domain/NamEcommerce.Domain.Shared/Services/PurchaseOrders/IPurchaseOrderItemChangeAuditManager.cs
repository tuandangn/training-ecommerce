using NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;

namespace NamEcommerce.Domain.Shared.Services.PurchaseOrders;

public interface IPurchaseOrderItemChangeAuditManager
{
    Task RecordAsync(CreatePurchaseOrderItemChangeAuditDto dto);

    Task<IReadOnlyList<PurchaseOrderItemChangeAuditDto>> GetByPurchaseOrderIdAsync(Guid purchaseOrderId);
}
