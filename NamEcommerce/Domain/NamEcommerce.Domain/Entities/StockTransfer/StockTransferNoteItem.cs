using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.StockTransfer;

[Serializable]
public sealed record StockTransferNoteItem : AppEntity
{
    public Guid NoteId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; }
    public decimal Quantity { get; private set; }

    /// <summary>AverageCost của kho nguồn tại thời điểm Approve — set bởi StockTransferNoteManager.</summary>
    public decimal UnitCost { get; internal set; }

    public StockTransferNoteItem(Guid id) : base(id)
    {
        ProductName = string.Empty;
    }

    internal StockTransferNoteItem(Guid noteId, Guid productId, string productName, decimal quantity) : base(Guid.NewGuid())
    {
        NoteId = noteId;
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
    }
}
