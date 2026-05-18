using NamEcommerce.Domain.Shared.Enums.StockTransfer;
using NamEcommerce.Domain.Shared.Exceptions;

namespace NamEcommerce.Domain.Shared.Dtos.StockTransfer;

[Serializable]
public sealed record StockTransferNoteDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required Guid FromWarehouseId { get; init; }
    public string? FromWarehouseName { get; init; }
    public required Guid ToWarehouseId { get; init; }
    public string? ToWarehouseName { get; init; }
    public string? Note { get; init; }
    public required StockTransferStatus Status { get; init; }
    public DateTime? ApprovedOnUtc { get; init; }
    public Guid? CreatedByUserId { get; init; }
    public required DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }
    public IList<StockTransferNoteItemDto> Items { get; init; } = [];
}

[Serializable]
public sealed record StockTransferNoteItemDto
{
    public required Guid Id { get; init; }
    public required Guid NoteId { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public required decimal Quantity { get; init; }
    public decimal UnitCost { get; init; }
}

[Serializable]
public sealed record CreateStockTransferNoteDto
{
    public required Guid FromWarehouseId { get; init; }
    public required Guid ToWarehouseId { get; init; }
    public string? Note { get; init; }
    public required IList<CreateStockTransferNoteItemDto> Items { get; init; }

    public void Verify()
    {
        if (FromWarehouseId == Guid.Empty)
            throw new NamEcommerceDomainException("Error.StockTransfer.FromWarehouseRequired");
        if (ToWarehouseId == Guid.Empty)
            throw new NamEcommerceDomainException("Error.StockTransfer.ToWarehouseRequired");
        if (FromWarehouseId == ToWarehouseId)
            throw new NamEcommerceDomainException("Error.StockTransfer.SameWarehouse");
        if (Items == null || Items.Count == 0)
            throw new NamEcommerceDomainException("Error.StockTransfer.ItemsRequired");
        if (Items.Any(i => i.ProductId == Guid.Empty))
            throw new NamEcommerceDomainException("Error.StockTransfer.ProductRequired");
        if (Items.Any(i => i.Quantity <= 0))
            throw new NamEcommerceDomainException("Error.StockTransfer.QuantityMustBePositive");
    }
}

[Serializable]
public sealed record CreateStockTransferNoteItemDto
{
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public required decimal Quantity { get; init; }
}
