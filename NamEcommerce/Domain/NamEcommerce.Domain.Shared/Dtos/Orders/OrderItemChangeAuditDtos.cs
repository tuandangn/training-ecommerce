using NamEcommerce.Domain.Shared.Enums.Orders;

namespace NamEcommerce.Domain.Shared.Dtos.Orders;

[Serializable]
public sealed record OrderItemChangeAuditDto(Guid Id)
{
    public required Guid OrderId { get; init; }
    public required Guid OrderItemId { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public required OrderItemChangeAuditAction Action { get; init; }
    public decimal? OldQuantity { get; init; }
    public decimal? NewQuantity { get; init; }
    public decimal? OldUnitPrice { get; init; }
    public decimal? NewUnitPrice { get; init; }
    public Guid? ChangedByUserId { get; init; }
    public string? ChangedByUsername { get; init; }
    public DateTime CreatedOnUtc { get; init; }
}

[Serializable]
public sealed record CreateOrderItemChangeAuditDto
{
    public required Guid OrderId { get; init; }
    public required Guid OrderItemId { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public required OrderItemChangeAuditAction Action { get; init; }
    public decimal? OldQuantity { get; init; }
    public decimal? NewQuantity { get; init; }
    public decimal? OldUnitPrice { get; init; }
    public decimal? NewUnitPrice { get; init; }
    public Guid? ChangedByUserId { get; init; }
    public string? ChangedByUsername { get; init; }

    public void Verify()
    {
        if (OrderId == Guid.Empty)
            throw new ArgumentException("Order is required.", nameof(OrderId));
        if (OrderItemId == Guid.Empty)
            throw new ArgumentException("Order item is required.", nameof(OrderItemId));
        if (ProductId == Guid.Empty)
            throw new ArgumentException("Product is required.", nameof(ProductId));
        if (string.IsNullOrWhiteSpace(ProductName))
            throw new ArgumentException("Product name is required.", nameof(ProductName));
        if (!Enum.IsDefined(Action))
            throw new ArgumentException("Action is invalid.", nameof(Action));

        if (Action == OrderItemChangeAuditAction.Added && (!NewQuantity.HasValue || !NewUnitPrice.HasValue))
            throw new ArgumentException("Added item audit requires new quantity and unit price.");
        if (Action == OrderItemChangeAuditAction.Updated
            && (!OldQuantity.HasValue || !NewQuantity.HasValue || !OldUnitPrice.HasValue || !NewUnitPrice.HasValue))
            throw new ArgumentException("Updated item audit requires old and new values.");
        if (Action == OrderItemChangeAuditAction.Removed && (!OldQuantity.HasValue || !OldUnitPrice.HasValue))
            throw new ArgumentException("Removed item audit requires old quantity and unit price.");
    }
}
