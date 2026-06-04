using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;
using NamEcommerce.Domain.Shared.Exceptions.PurchaseOrders;

namespace NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;

[Serializable]
public sealed record PurchaseOrderItemAllocationDto(Guid Id)
{
    public required Guid PurchaseOrderItemId { get; init; }
    public required Guid OrderItemId { get; init; }
    public required decimal AllocatedQuantity { get; init; }
    public required decimal ReceivedQuantity { get; init; }
    public required AllocationStatus Status { get; init; }
    public bool IsDirectShip { get; init; }
    public string? DirectShipAddress { get; init; }
    public string? DirectShipContactName { get; init; }
    public string? DirectShipContactPhone { get; init; }
    public int DirectShipPriority { get; init; }
    public required DateTime CreatedOnUtc { get; init; }
}

[Serializable]
public sealed record AllocatePurchaseOrderItemForOrder
{
    public SecondaryItemId PurchaseOrderItemId { get; set; }
    public SecondaryItemId OrderItemId { get; set; }
    public decimal AllocationQuantity { get; set; }
    public AllocateDirectShipInfo? DirectShipInfo { get; set; }

    public void Verify()
    {
        if (AllocationQuantity <= 0)
            throw new PurchaseOrderItemAllocationDataIsInvalidException("Error.AllocatedQuantityMustBePositive", "Label.Quantity");

        if (DirectShipInfo is not null)
            DirectShipInfo.Verify();
    }

    [Serializable]
    public sealed record AllocateDirectShipInfo(string? ContactName, string ContactPhone, string? Address)
    {
        public int Priority { get; set; }

        public void Verify()
        {
            if (string.IsNullOrWhiteSpace(ContactPhone))
                throw new DirectShipDataIsInvalidException("Error.DirectShipContactPhoneRequired");
        }
    }
}

[Serializable]
public sealed record EligibleOrderItemForAllocationDto
{
    public required Guid OrderItemId { get; init; }
    public required Guid OrderId { get; init; }
    public required string OrderCode { get; init; }
    public required string CustomerName { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public int QuantityDecimalPlaces { get; init; }
    public required decimal TotalQuantity { get; init; }
    public required decimal AllocatedOutstanding { get; init; }
    public required decimal AvailableToAllocate { get; init; }
    public string? ShippingAddress { get; init; }
    public string? CustomerPhone { get; init; }
}

[Serializable]
public sealed record NonDirectShipAllocationDto
{
    public required Guid AllocationId { get; init; }
    public required Guid OrderId { get; init; }
    public required Guid OrderItemId { get; init; }
    public required string OrderCode { get; init; }
    public required string CustomerName { get; init; }
    public required decimal AllocatedQuantity { get; init; }
    public required decimal RemainingQuantity { get; init; }
    public string? CustomerPhone { get; init; }
    public string? ShippingAddress { get; init; }
}

[Serializable]
public sealed record AllocationReceiptDto
{
    public required Guid AllocationId { get; init; }
    public required Guid OrderItemId { get; init; }
    public required decimal Quantity { get; init; }
    public required bool IsDirectShip { get; init; }
    public string? DirectShipAddress { get; init; }
}

[Serializable]
public sealed record DistributeReceivedQuantityResultDto
{
    public required IReadOnlyList<AllocationReceiptDto> DirectShipReceipts { get; init; }
    public required IReadOnlyList<AllocationReceiptDto> WarehouseReceipts { get; init; }
}
