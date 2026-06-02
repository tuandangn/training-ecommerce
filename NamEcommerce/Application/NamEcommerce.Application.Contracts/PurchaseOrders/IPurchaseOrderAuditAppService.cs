using NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;

namespace NamEcommerce.Application.Contracts.PurchaseOrders;

public interface IPurchaseOrderAuditAppService
{
    Task<IReadOnlyList<PurchaseOrderItemChangeAuditAppDto>> GetPurchaseOrderItemChangeAuditsAsync(Guid purchaseOrderId);
}
