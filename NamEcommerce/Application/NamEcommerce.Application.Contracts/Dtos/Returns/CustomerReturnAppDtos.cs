namespace NamEcommerce.Application.Contracts.Dtos.Returns;

[Serializable]
public sealed record CustomerReturnAppDto(Guid Id)
{
    public required string Code { get; init; }
    public required Guid OrderId { get; init; }
    public required string OrderCode { get; init; }
    public required Guid CustomerId { get; init; }
    public required string CustomerName { get; init; }
    public required Guid WarehouseId { get; init; }
    public required string WarehouseName { get; init; }
    public string? Note { get; init; }
    public required int Status { get; init; }
    public required DateTime ReturnDate { get; init; }
    public DateTime? ConfirmedOnUtc { get; init; }
    public Guid? GeneratedGoodsReceiptId { get; init; }
    public Guid? CreatedByUserId { get; init; }
    public required DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }
    public required IEnumerable<CustomerReturnItemAppDto> Items { get; init; }
}

[Serializable]
public sealed record CustomerReturnItemAppDto(Guid Id)
{
    public required Guid CustomerReturnId { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public Guid? DeliveryNoteItemId { get; init; }
    public required decimal RequestedQuantity { get; init; }
    public required decimal AcceptedQuantity { get; init; }
    public required decimal UnitPrice { get; init; }
    public decimal AcceptedTotal => AcceptedQuantity * UnitPrice;
}

[Serializable]
public sealed record CreateCustomerReturnAppDto
{
    public required Guid OrderId { get; init; }
    public required Guid WarehouseId { get; init; }
    public string? Note { get; init; }
    public required IEnumerable<CreateCustomerReturnItemAppDto> Items { get; init; }

    public (bool valid, string? errorMessage) Validate()
    {
        if (OrderId == Guid.Empty)
            return (false, "Error.CustomerReturn.OrderRequired");
        if (WarehouseId == Guid.Empty)
            return (false, "Error.CustomerReturn.WarehouseRequired");
        if (!Items.Any())
            return (false, "Error.CustomerReturn.NoItems");
        return (true, null);
    }
}

[Serializable]
public sealed record CreateCustomerReturnItemAppDto
{
    public required Guid ProductId { get; init; }
    public Guid? DeliveryNoteItemId { get; init; }
    public required decimal RequestedQuantity { get; init; }
    public required decimal AcceptedQuantity { get; init; }
    public required decimal UnitPrice { get; init; }
}

[Serializable]
public sealed record UpdateCustomerReturnAppDto(Guid Id)
{
    public string? Note { get; init; }
    public DateTime? ReturnDate { get; init; }
}

[Serializable]
public sealed record CreateCustomerReturnResultAppDto
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid? CreatedId { get; init; }
}

[Serializable]
public sealed record UpdateCustomerReturnResultAppDto
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}

[Serializable]
public sealed record ConfirmCustomerReturnResultAppDto
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}
