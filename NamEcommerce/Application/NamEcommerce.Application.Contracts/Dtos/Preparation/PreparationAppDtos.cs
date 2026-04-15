namespace NamEcommerce.Application.Contracts.Dtos.Preparation;

/// <summary>
/// Ungrouped preparation item - one row per OrderItem from active (non-locked) orders.
/// Default view for the preparation dashboard.
/// </summary>
[Serializable]
public sealed record PreparationItemAppDto
{
    public required Guid OrderItemId { get; init; }
    public required Guid OrderId { get; init; }
    public required Guid CustomerId { get; init; }
    public required string OrderCode { get; init; }

    public required Guid ProductId { get; init; }

    public required decimal Quantity { get; init; }
    public required decimal UnitPrice { get; init; }

    public DateTime? ExpectedShippingDateUtc { get; set; }

    public bool IsDelivered { get; init; }
}

/// <summary>
/// Grouped preparation item - products grouped by ProductId across multiple orders.
/// Shows total quantity needed and list of customers who ordered.
/// </summary>
[Serializable]
public sealed record PreparationGroupedItemAppDto
{
    public required Guid ProductId { get; init; }

    public required decimal TotalQuantity { get; init; }
    public DateTime? EarliestShippingDate { get; init; }

    public required IList<PreparationGroupedCustomerDetail> CustomerDetails { get; init; }
}

[Serializable]
public sealed record PreparationGroupedCustomerDetail
{
    public required Guid OrderItemId { get; init; }
    public required Guid OrderId { get; init; }
    public required string OrderCode { get; init; }

    public required Guid CustomerId { get; init; }

    public required decimal Quantity { get; init; }
    public DateTime? ExpectedShippingDateUtc { get; set; }

    public bool IsDelivered { get; init; }
}
