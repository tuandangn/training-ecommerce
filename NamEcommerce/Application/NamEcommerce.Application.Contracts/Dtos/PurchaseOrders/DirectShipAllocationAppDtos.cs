namespace NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;

[Serializable]
public sealed record DirectShipAllocationStatusAppDto
{
    public Guid AllocationId { get; init; }
    public Guid OrderItemId { get; init; }
    public int Status { get; init; }
    public int? DeliveryStatus { get; init; }
    public Guid? DeliveryNoteId { get; init; }
    public string? DeliveryNoteCode { get; init; }
    public decimal AllocatedQuantity { get; init; }
    public decimal ReceivedQuantity { get; init; }
}

[Serializable]
public sealed record DirectShipAllocationForPoItemAppDto
{
    public Guid AllocationId { get; init; }
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
