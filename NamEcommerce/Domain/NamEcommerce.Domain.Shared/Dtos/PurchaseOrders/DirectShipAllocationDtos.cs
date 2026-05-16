namespace NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;

[Serializable]
public sealed record DirectShipAllocationStatusDto
{
    public Guid AllocationId { get; init; }
    public Guid OrderItemId { get; init; }
    public int Status { get; init; }
    public decimal AllocatedQuantity { get; init; }
}
