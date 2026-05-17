namespace NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;

[Serializable]
public sealed record DirectShipAllocationStatusDto
{
    public Guid AllocationId { get; init; }
    public Guid OrderItemId { get; init; }
    public int Status { get; init; }
    public decimal AllocatedQuantity { get; init; }
}

[Serializable]
public sealed record DirectShipAllocationForPoItemDto
{
    public Guid AllocationId { get; init; }
    public Guid PurchaseOrderItemId { get; init; }
    public string DirectShipAddress { get; init; } = string.Empty;
    public string? DirectShipContactName { get; init; }
    public string? DirectShipContactPhone { get; init; }
    public decimal AllocatedQuantity { get; init; }
    public int Status { get; init; }
}
