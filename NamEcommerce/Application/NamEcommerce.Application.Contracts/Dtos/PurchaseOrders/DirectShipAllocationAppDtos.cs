namespace NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;

[Serializable]
public sealed record DirectShipAllocationStatusAppDto
{
    public Guid AllocationId { get; init; }
    public Guid OrderItemId { get; init; }
    public int Status { get; init; }
    public decimal AllocatedQuantity { get; init; }
}
