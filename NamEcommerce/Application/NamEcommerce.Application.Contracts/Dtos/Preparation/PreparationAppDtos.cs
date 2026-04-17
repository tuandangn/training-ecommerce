namespace NamEcommerce.Application.Contracts.Dtos.Preparation;


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
    
    public decimal DeliveredQuantity { get; init; }
    
    public decimal StockQuantityAvailable { get; set; }

    public IList<DeliveryNoteLinkAppDto> DeliveryNoteLinks { get; set; } = [];
}

[Serializable]
public sealed record PreparationGroupedItemAppDto
{
    public required Guid ProductId { get; init; }

    public required decimal TotalQuantity { get; init; }
    public DateTime? EarliestShippingDate { get; init; }

    public required IList<PreparationGroupedCustomerDetail> CustomerDetails { get; init; }

    public decimal StockQuantityAvailable { get; set; }
}

[Serializable]
public sealed record PreparationGroupedCustomerDetail
{
    public required Guid OrderItemId { get; init; }
    public required Guid OrderId { get; init; }
    public required string OrderCode { get; init; }

    public required Guid CustomerId { get; init; }

    public required decimal Quantity { get; init; }
    public required decimal UnitPrice { get; set; }
    public DateTime? ExpectedShippingDateUtc { get; set; }

    public bool IsDelivered { get; init; }

    public decimal DeliveredQuantity { get; init; }

    public IList<DeliveryNoteLinkAppDto> DeliveryNoteLinks { get; set; } = [];
}

[Serializable]
public sealed record DeliveryNoteLinkAppDto(Guid Id, string Code, int Status, DateTime CreatedOnUtc);
