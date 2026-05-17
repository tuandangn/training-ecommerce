namespace NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;

[Serializable]
public sealed record DirectShipAllocationStatusAppDto
{
    public Guid AllocationId { get; init; }
    public Guid OrderItemId { get; init; }
    public int Status { get; init; }
    public decimal AllocatedQuantity { get; init; }
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
    public int Status { get; init; }
}
