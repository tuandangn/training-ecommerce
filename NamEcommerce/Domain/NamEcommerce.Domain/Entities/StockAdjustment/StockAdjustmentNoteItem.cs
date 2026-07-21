using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.StockAdjustment;

[Serializable]
public sealed record StockAdjustmentNoteItem : AppEntity
{
    internal StockAdjustmentNoteItem(Guid noteId, Guid productId, string productName,
        decimal systemQuantity, decimal physicalQuantity) : base(Guid.Empty)
    {
        NoteId = noteId;
        ProductId = productId;
        ProductName = productName;
        SystemQuantity = systemQuantity;
        PhysicalQuantity = physicalQuantity;
    }

    public Guid NoteId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; }

    /// <summary>Số lượng hệ thống đang ghi tại thời điểm tạo phiếu (snapshot).</summary>
    public decimal SystemQuantity { get; private set; }

    /// <summary>Số lượng kiểm kê thực tế.</summary>
    public decimal PhysicalQuantity { get; internal set; }

    /// <summary>Delta = PhysicalQuantity − SystemQuantity. Dương → cộng tồn; âm → trừ tồn.</summary>
    public decimal Delta => PhysicalQuantity - SystemQuantity;

    public StockAdjustmentNoteItem(Guid id) : base(id)
    {
        ProductName = string.Empty;
    }
}
