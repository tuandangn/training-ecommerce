namespace NamEcommerce.Web.Contracts.Models.StockAdjustment;

public sealed class StockAdjustmentNoteModel
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public string? Note { get; set; }
    public int Status { get; set; }
    public DateTime? ApprovedOnUtc { get; set; }
    public DateTime CreatedOnUtc { get; set; }
    public List<StockAdjustmentNoteItemModel> Items { get; set; } = [];
}

public sealed class StockAdjustmentNoteItemModel
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal SystemQuantity { get; set; }
    public decimal PhysicalQuantity { get; set; }
    public decimal Delta => PhysicalQuantity - SystemQuantity;
}

public sealed class StockAdjustmentNoteListModel
{
    public List<StockAdjustmentNoteListItemModel> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}

public sealed class StockAdjustmentNoteListItemModel
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? WarehouseName { get; set; }
    public int Status { get; set; }
    public int ItemCount { get; set; }
    public DateTime CreatedOnUtc { get; set; }
}

public sealed class StockAdjustmentNoteItemInputModel
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal SystemQuantity { get; set; }
    public decimal PhysicalQuantity { get; set; }
    public int QuantityDecimalPlaces { get; set; }
}

public sealed class CreateStockAdjustmentNoteResultModel
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? CreatedId { get; set; }
}

public sealed class StockAdjustmentNoteActionResultModel
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
