namespace NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;

[Serializable]
public sealed record DirectShipAllocationStatusDto
{
    public Guid AllocationId { get; init; }
    public Guid OrderId { get; init; }
    public Guid OrderItemId { get; init; }
    public int Status { get; init; }
    public int? DeliveryStatus { get; init; }
    public Guid? DeliveryNoteId { get; init; }
    public string? DeliveryNoteCode { get; init; }
    public decimal AllocatedQuantity { get; init; }
    public decimal ReceivedQuantity { get; init; }
}

[Serializable]
public sealed record DirectShipAllocationForPoItemDto
{
    public Guid AllocationId { get; init; }
    public Guid PurchaseOrderId { get; init; }
    public Guid PurchaseOrderItemId { get; init; }
    public string DirectShipAddress { get; init; } = string.Empty;
    public string? DirectShipContactName { get; init; }
    public string? DirectShipContactPhone { get; init; }
    public decimal AllocatedQuantity { get; init; }
    public decimal ReceivedQuantity { get; init; }
    public int Status { get; init; }
    public int? DeliveryStatus { get; init; }
    public Guid? DeliveryNoteId { get; init; }
    public string? DeliveryNoteCode { get; init; }
}
