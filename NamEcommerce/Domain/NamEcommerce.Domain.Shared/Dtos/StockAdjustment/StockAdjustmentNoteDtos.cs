using NamEcommerce.Domain.Shared.Enums.StockAdjustment;
using NamEcommerce.Domain.Shared.Exceptions;

namespace NamEcommerce.Domain.Shared.Dtos.StockAdjustment;

[Serializable]
public sealed record StockAdjustmentNoteDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required Guid WarehouseId { get; init; }
    public string? WarehouseName { get; init; }
    public string? Note { get; init; }
    public required StockAdjustmentStatus Status { get; init; }
    public DateTime? ApprovedOnUtc { get; init; }
    public Guid? CreatedByUserId { get; init; }
    public required DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }
    public IList<StockAdjustmentNoteItemDto> Items { get; init; } = [];
}

[Serializable]
public sealed record StockAdjustmentNoteItemDto
{
    public required Guid Id { get; init; }
    public required Guid NoteId { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    /// <summary>Số lượng hệ thống đang ghi (snapshot tại lúc tạo item).</summary>
    public required decimal SystemQuantity { get; init; }
    /// <summary>Số lượng kiểm kê thực tế.</summary>
    public required decimal PhysicalQuantity { get; init; }
    /// <summary>Delta = PhysicalQuantity - SystemQuantity. Dương → nhập thêm; âm → xuất bớt.</summary>
    public decimal Delta => PhysicalQuantity - SystemQuantity;
}

[Serializable]
public sealed record CreateStockAdjustmentNoteDto
{
    public required Guid WarehouseId { get; init; }
    public string? Note { get; init; }
    public required IList<CreateStockAdjustmentNoteItemDto> Items { get; init; }

    public void Verify()
    {
        if (WarehouseId == Guid.Empty)
            throw new NamEcommerceDomainException("Error.StockAdjustment.WarehouseRequired");
        if (Items == null || Items.Count == 0)
            throw new NamEcommerceDomainException("Error.StockAdjustment.ItemsRequired");
        if (Items.Any(i => i.ProductId == Guid.Empty))
            throw new NamEcommerceDomainException("Error.StockAdjustment.ProductRequired");
        if (Items.Any(i => i.PhysicalQuantity < 0))
            throw new NamEcommerceDomainException("Error.StockAdjustment.PhysicalQuantityCannotBeNegative");
    }
}

[Serializable]
public sealed record CreateStockAdjustmentNoteItemDto
{
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    /// <summary>Số lượng hệ thống đang ghi — UI pre-fill, Manager validate.</summary>
    public required decimal SystemQuantity { get; init; }
    /// <summary>Số lượng kiểm kê thực tế do người dùng nhập.</summary>
    public required decimal PhysicalQuantity { get; init; }
}
