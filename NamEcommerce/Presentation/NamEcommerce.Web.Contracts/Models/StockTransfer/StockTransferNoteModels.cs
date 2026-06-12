namespace NamEcommerce.Web.Contracts.Models.StockTransfer;

public sealed class StockTransferNoteModel
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public Guid FromWarehouseId { get; set; }
    public string? FromWarehouseName { get; set; }
    public Guid ToWarehouseId { get; set; }
    public string? ToWarehouseName { get; set; }
    public string? Note { get; set; }
    public int Status { get; set; }
    public DateTime? ApprovedOnUtc { get; set; }
    public DateTime CreatedOnUtc { get; set; }
    public List<StockTransferNoteItemModel> Items { get; set; } = [];
}

public sealed class StockTransferNoteItemModel
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
}

public sealed class StockTransferNoteListModel
{
    public List<StockTransferNoteListItemModel> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}

public sealed class StockTransferNoteListItemModel
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? FromWarehouseName { get; set; }
    public string? ToWarehouseName { get; set; }
    public int Status { get; set; }
    public int ItemCount { get; set; }
    public DateTime CreatedOnUtc { get; set; }
}

public sealed class StockTransferNoteItemInputModel
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public int QuantityDecimalPlaces { get; set; }
}

public sealed class CreateStockTransferNoteResultModel : ICommandResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? CreatedId { get; set; }
}

public sealed class StockTransferNoteActionResultModel : ICommandResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
