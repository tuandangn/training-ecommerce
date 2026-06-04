using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;

namespace NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;

[Serializable]
public sealed record PurchaseOrderItemChangeAuditDto(Guid Id)
{
    public required Guid PurchaseOrderId { get; init; }
    public required Guid PurchaseOrderItemId { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public required PurchaseOrderItemChangeAuditAction Action { get; init; }
    public decimal? OldQuantity { get; init; }
    public decimal? NewQuantity { get; init; }
    public decimal? OldUnitCost { get; init; }
    public decimal? NewUnitCost { get; init; }
    public string? OldNote { get; init; }
    public string? NewNote { get; init; }
    public Guid? ChangedByUserId { get; init; }
    public string? ChangedByUsername { get; init; }
    public DateTime CreatedOnUtc { get; init; }
}

[Serializable]
public sealed record CreatePurchaseOrderItemChangeAuditDto
{
    public required Guid PurchaseOrderId { get; init; }
    public required Guid PurchaseOrderItemId { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public required PurchaseOrderItemChangeAuditAction Action { get; init; }
    public decimal? OldQuantity { get; init; }
    public decimal? NewQuantity { get; init; }
    public decimal? OldUnitCost { get; init; }
    public decimal? NewUnitCost { get; init; }
    public string? OldNote { get; init; }
    public string? NewNote { get; init; }
    public Guid? ChangedByUserId { get; init; }
    public string? ChangedByUsername { get; init; }

    public void Verify()
    {
        if (PurchaseOrderId == Guid.Empty)
            throw new ArgumentException("Purchase order is required.", nameof(PurchaseOrderId));
        if (PurchaseOrderItemId == Guid.Empty)
            throw new ArgumentException("Purchase order item is required.", nameof(PurchaseOrderItemId));
        if (ProductId == Guid.Empty)
            throw new ArgumentException("Product is required.", nameof(ProductId));
        if (string.IsNullOrWhiteSpace(ProductName))
            throw new ArgumentException("Product name is required.", nameof(ProductName));
        if (!Enum.IsDefined(Action))
            throw new ArgumentException("Action is invalid.", nameof(Action));

        if (Action == PurchaseOrderItemChangeAuditAction.Added && (!NewQuantity.HasValue || !NewUnitCost.HasValue))
            throw new ArgumentException("Added item audit requires new quantity and unit cost.");
        if (Action == PurchaseOrderItemChangeAuditAction.Updated
            && (!OldQuantity.HasValue || !NewQuantity.HasValue || !OldUnitCost.HasValue || !NewUnitCost.HasValue))
            throw new ArgumentException("Updated item audit requires old and new values.");
        if (Action == PurchaseOrderItemChangeAuditAction.Removed && (!OldQuantity.HasValue || !OldUnitCost.HasValue))
            throw new ArgumentException("Removed item audit requires old quantity and unit cost.");
    }
}
