using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;

namespace NamEcommerce.Domain.Services.Extensions;

public static class PurchaseOrderItemChangeAuditExtensions
{
    public static PurchaseOrderItemChangeAuditDto ToDto(this PurchaseOrderItemChangeAudit audit)
        => new(audit.Id)
        {
            PurchaseOrderId = audit.PurchaseOrderId,
            PurchaseOrderItemId = audit.PurchaseOrderItemId,
            ProductId = audit.ProductId,
            ProductName = audit.ProductName,
            Action = audit.Action,
            OldQuantity = audit.OldQuantity,
            NewQuantity = audit.NewQuantity,
            OldUnitCost = audit.OldUnitCost,
            NewUnitCost = audit.NewUnitCost,
            OldNote = audit.OldNote,
            NewNote = audit.NewNote,
            ChangedByUserId = audit.ChangedByUserId,
            ChangedByUsername = audit.ChangedByUsername,
            CreatedOnUtc = audit.CreatedOnUtc
        };
}
