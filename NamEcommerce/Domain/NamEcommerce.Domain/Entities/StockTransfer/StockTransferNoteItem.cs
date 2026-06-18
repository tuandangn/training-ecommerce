using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.StockTransfer;

[Serializable]
public sealed record StockTransferNoteItem : AppEntity
{
    private StockTransferNoteItem(Guid id) : base(id)
    {
        ProductName = string.Empty;
    }

    internal StockTransferNoteItem(Guid noteId, Guid productId, string productName, decimal quantity) : base(Guid.Empty)
    {
        NoteId = noteId;
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
    }

    public Guid NoteId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; }
    public decimal Quantity { get; private set; }

    /// <summary>Cost hiển thị tại thời điểm Approve — nguồn đúng là InventoryCostLedger/Allocation.</summary>
    public decimal UnitCost { get; internal set; }
}
