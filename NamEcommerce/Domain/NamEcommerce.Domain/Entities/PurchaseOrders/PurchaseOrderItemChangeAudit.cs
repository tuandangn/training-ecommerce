using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;
using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;

namespace NamEcommerce.Domain.Entities.PurchaseOrders;

[Serializable]
public sealed record PurchaseOrderItemChangeAudit : AppAggregateEntity
{
    internal PurchaseOrderItemChangeAudit(
        Guid id,
        Guid purchaseOrderId,
        Guid purchaseOrderItemId,
        Guid productId,
        string productName,
        PurchaseOrderItemChangeAuditAction action,
        decimal? oldQuantity,
        decimal? newQuantity,
        decimal? oldUnitCost,
        decimal? newUnitCost,
        string? oldNote,
        string? newNote,
        Guid? changedByUserId,
        string? changedByUsername,
        DateTime createdOnUtc) : base(id)
    {
        PurchaseOrderId = purchaseOrderId;
        PurchaseOrderItemId = purchaseOrderItemId;
        ProductId = productId;
        ProductName = productName;
        Action = action;
        OldQuantity = oldQuantity;
        NewQuantity = newQuantity;
        OldUnitCost = oldUnitCost;
        NewUnitCost = newUnitCost;
        OldNote = oldNote;
        NewNote = newNote;
        ChangedByUserId = changedByUserId;
        ChangedByUsername = changedByUsername;
        CreatedOnUtc = createdOnUtc;
    }

    public Guid PurchaseOrderId { get; init; }
    public Guid PurchaseOrderItemId { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; }
    public PurchaseOrderItemChangeAuditAction Action { get; init; }
    public decimal? OldQuantity { get; init; }
    public decimal? NewQuantity { get; init; }
    public decimal? OldUnitCost { get; init; }
    public decimal? NewUnitCost { get; init; }
    public string? OldNote { get; init; }
    public string? NewNote { get; init; }
    public Guid? ChangedByUserId { get; init; }
    public string? ChangedByUsername { get; init; }
    public DateTime CreatedOnUtc { get; init; }

    internal static PurchaseOrderItemChangeAudit Create(CreatePurchaseOrderItemChangeAuditDto dto)
    {
        dto.Verify();

        return new PurchaseOrderItemChangeAudit(
            Guid.NewGuid(),
            dto.PurchaseOrderId,
            dto.PurchaseOrderItemId,
            dto.ProductId,
            dto.ProductName,
            dto.Action,
            dto.OldQuantity,
            dto.NewQuantity,
            dto.OldUnitCost,
            dto.NewUnitCost,
            dto.OldNote,
            dto.NewNote,
            dto.ChangedByUserId,
            dto.ChangedByUsername,
            DateTime.UtcNow);
    }
}
