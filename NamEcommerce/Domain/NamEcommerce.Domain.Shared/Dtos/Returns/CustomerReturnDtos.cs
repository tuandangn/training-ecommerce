using NamEcommerce.Domain.Shared.Exceptions.Returns;

namespace NamEcommerce.Domain.Shared.Dtos.Returns;

[Serializable]
public sealed record CustomerReturnDto(Guid Id)
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
    public required IEnumerable<CustomerReturnItemDto> Items { get; init; }
}

[Serializable]
public sealed record CustomerReturnItemDto(Guid Id)
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
public sealed record CreateCustomerReturnDto
{
    public required Guid OrderId { get; init; }
    public required Guid WarehouseId { get; init; }
    public string? Note { get; init; }
    public required IEnumerable<CreateCustomerReturnItemDto> Items { get; init; }

    public void Verify()
    {
        if (!Items.Any())
            throw new ReturnDataIsInvalidException("Error.CustomerReturn.NoItems");
        foreach (var item in Items)
        {
            if (item.RequestedQuantity <= 0)
                throw new ReturnDataIsInvalidException("Error.CustomerReturn.RequestedQuantityMustBePositive");
            if (item.AcceptedQuantity < 0)
                throw new ReturnDataIsInvalidException("Error.CustomerReturn.AcceptedQuantityCannotBeNegative");
        }
    }
}

[Serializable]
public sealed record CreateCustomerReturnItemDto
{
    public required Guid ProductId { get; init; }
    public Guid? DeliveryNoteItemId { get; init; }
    public required decimal RequestedQuantity { get; init; }
    public required decimal AcceptedQuantity { get; init; }
    public required decimal UnitPrice { get; init; }
}

[Serializable]
public sealed record UpdateCustomerReturnDto(Guid Id)
{
    public string? Note { get; init; }
    public DateTime? ReturnDate { get; init; }
    public IEnumerable<CreateCustomerReturnItemDto> Items { get; init; } = [];
}
