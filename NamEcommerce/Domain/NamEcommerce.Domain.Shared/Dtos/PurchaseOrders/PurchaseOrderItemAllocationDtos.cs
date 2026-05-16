namespace NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;

[Serializable]
public sealed record PurchaseOrderItemAllocationDto(Guid Id)
{
    public required Guid PurchaseOrderItemId { get; init; }
    public required Guid OrderItemId { get; init; }
    public required decimal AllocatedQuantity { get; init; }
    public required decimal ReceivedQuantity { get; init; }
    public required DateTime CreatedOnUtc { get; init; }
}
