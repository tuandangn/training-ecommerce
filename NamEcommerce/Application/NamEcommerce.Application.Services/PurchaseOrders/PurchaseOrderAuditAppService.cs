using NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Domain.Shared.Services.PurchaseOrders;

namespace NamEcommerce.Application.Services.PurchaseOrders;

public sealed class PurchaseOrderAuditAppService(
    IPurchaseOrderItemChangeAuditManager purchaseOrderItemChangeAuditManager) : IPurchaseOrderAuditAppService
{
    public async Task<IReadOnlyList<PurchaseOrderItemChangeAuditAppDto>> GetPurchaseOrderItemChangeAuditsAsync(Guid purchaseOrderId)
    {
        var audits = await purchaseOrderItemChangeAuditManager.GetByPurchaseOrderIdAsync(purchaseOrderId).ConfigureAwait(false);

        return audits.Select(audit => new PurchaseOrderItemChangeAuditAppDto(audit.Id)
        {
            PurchaseOrderId = audit.PurchaseOrderId,
            PurchaseOrderItemId = audit.PurchaseOrderItemId,
            ProductId = audit.ProductId,
            ProductName = audit.ProductName,
            Action = (int)audit.Action,
            OldQuantity = audit.OldQuantity,
            NewQuantity = audit.NewQuantity,
            OldUnitCost = audit.OldUnitCost,
            NewUnitCost = audit.NewUnitCost,
            OldNote = audit.OldNote,
            NewNote = audit.NewNote,
            ChangedByUserId = audit.ChangedByUserId,
            ChangedByUsername = audit.ChangedByUsername,
            CreatedOnUtc = audit.CreatedOnUtc
        }).ToList();
    }
}
