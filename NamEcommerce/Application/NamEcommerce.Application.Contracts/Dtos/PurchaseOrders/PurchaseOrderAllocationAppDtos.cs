namespace NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;

[Serializable]
public sealed record EligibleOrderItemForAllocationAppDto
{
    public required Guid OrderItemId { get; init; }
    public required Guid OrderId { get; init; }
    public required string OrderCode { get; init; }
    public required string CustomerName { get; init; }
    public string? CustomerPhone { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public string UnitMeasurement { get; set; }
    public int QuantityDecimalPlaces { get; init; }
    public required decimal TotalQuantity { get; init; }
    public required decimal AllocatedOutstanding { get; init; }
    public required decimal AvailableToAllocate { get; init; }
    public string? ShippingContactName { get; init; }
    public string? ShippingAddress { get; init; }
    public string? ShippingPhoneNumber { get; init; }
}

[Serializable]
public sealed record PurchaseOrderItemAllocationForPoItemAppDto
{
    public required Guid AllocationId { get; init; }
    public required Guid PurchaseOrderId { get; init; }
    public required Guid PurchaseOrderItemId { get; init; }
    public required Guid OrderId { get; init; }
    public required Guid OrderItemId { get; init; }
    public required string OrderCode { get; init; }
    public string? CustomerName { get; init; }
    public string? CustomerPhone { get; init; }
    public string? ShippingAddress { get; init; }
    public required decimal AllocatedQuantity { get; init; }
    public required decimal ReceivedQuantity { get; init; }
    public required int Status { get; init; }
    public required bool IsDirectShip { get; init; }
}

[Serializable]
public sealed record NonDirectShipAllocationForPoItemAppDto
{
    public required Guid AllocationId { get; init; }
    public required Guid OrderId { get; init; }
    public required Guid OrderItemId { get; init; }
    public required string OrderCode { get; init; }
    public required string CustomerName { get; init; }
    public required decimal AllocatedQuantity { get; init; }
    public required decimal RemainingQuantity { get; init; }
    public string? CustomerPhone { get; init; }
    public string? ShippingContactName { get; init; }
    public string? ShippingAddress { get; init; }
    public string? ShippingPhoneNumber { get; init; }
}

[Serializable]
public sealed record AllocatePoItemForOrderItemAppDto
{
    public required Guid PurchaseOrderId { get; set; }
    public required Guid PurchaseOrderItemId { get; init; }
    public required Guid OrderId { get; set; }
    public required Guid OrderItemId { get; init; }
    public required decimal Quantity { get; init; }
    public string? DirectShipAddress { get; init; }
    public string? DirectShipContactName { get; init; }
    public string? DirectShipContactPhone { get; init; }
}

[Serializable]
public sealed record ReleaseAllocationsOfPurchaseOrderItemAppDto
{
    public required Guid PurchaseOrderId { get; set; }
    public required Guid PurchaseOrderItemId { get; init; }
}

