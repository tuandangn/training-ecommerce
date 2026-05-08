using NamEcommerce.Domain.Shared.Exceptions.Returns;

namespace NamEcommerce.Domain.Shared.Dtos.Returns;

[Serializable]
public sealed record VendorReturnDto(Guid Id)
{
    public required string Code { get; init; }
    public required Guid VendorId { get; init; }
    public required string VendorName { get; init; }
    public Guid? PurchaseOrderId { get; init; }
    public Guid? GoodsReceiptId { get; init; }
    public required Guid WarehouseId { get; init; }
    public required string WarehouseName { get; init; }
    public string? Note { get; init; }
    public required int Status { get; init; }
    public required DateTime ReturnDate { get; init; }
    public DateTime? ConfirmedOnUtc { get; init; }
    public Guid? GeneratedDeliveryNoteId { get; init; }
    public Guid? CreatedByUserId { get; init; }
    public required DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }
    public required IEnumerable<VendorReturnItemDto> Items { get; init; }
}

[Serializable]
public sealed record VendorReturnItemDto(Guid Id)
{
    public required Guid VendorReturnId { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public Guid? GoodsReceiptItemId { get; init; }
    public required decimal RequestedQuantity { get; init; }
    public required decimal AcceptedQuantity { get; init; }
    public required decimal UnitCost { get; init; }
    public decimal AcceptedTotal => AcceptedQuantity * UnitCost;
}

[Serializable]
public sealed record CreateVendorReturnDto
{
    public required Guid VendorId { get; init; }
    public Guid? PurchaseOrderId { get; init; }
    public Guid? GoodsReceiptId { get; init; }
    public required Guid WarehouseId { get; init; }
    public string? Note { get; init; }
    public required IEnumerable<CreateVendorReturnItemDto> Items { get; init; }

    public void Verify()
    {
        if (PurchaseOrderId is null && GoodsReceiptId is null)
            throw new ReturnDataIsInvalidException("Error.VendorReturn.PurchaseOrderOrGoodsReceiptRequired");
        if (!Items.Any())
            throw new ReturnDataIsInvalidException("Error.VendorReturn.NoItems");
        foreach (var item in Items)
        {
            if (item.RequestedQuantity <= 0)
                throw new ReturnDataIsInvalidException("Error.VendorReturn.RequestedQuantityMustBePositive");
            if (item.AcceptedQuantity < 0)
                throw new ReturnDataIsInvalidException("Error.VendorReturn.AcceptedQuantityCannotBeNegative");
        }
    }
}

[Serializable]
public sealed record CreateVendorReturnItemDto
{
    public required Guid ProductId { get; init; }
    public Guid? GoodsReceiptItemId { get; init; }
    public required decimal RequestedQuantity { get; init; }
    public required decimal AcceptedQuantity { get; init; }
    public required decimal UnitCost { get; init; }
}

[Serializable]
public sealed record UpdateVendorReturnDto(Guid Id)
{
    public string? Note { get; init; }
    public DateTime? ReturnDate { get; init; }
    public IEnumerable<CreateVendorReturnItemDto> Items { get; init; } = [];
}
