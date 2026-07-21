using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.DeliveryNotes;

/// <summary>
/// Snapshot số lượng nhận/trả shipper đề xuất lúc gửi duyệt thu hụt.
/// Đọc lại lúc completion để dựng acceptance (tạo CustomerReturn + công nợ).
/// </summary>
public sealed record DeliveryNoteSettlementItem : AppEntity
{
    private DeliveryNoteSettlementItem(Guid id) : base(id)
    {
    }

    internal DeliveryNoteSettlementItem(
        Guid deliveryNoteId, Guid deliveryNoteItemId,
        decimal acceptedQuantity, decimal rejectedQuantity, string? rejectReason) : base(Guid.Empty)
    {
        DeliveryNoteId = deliveryNoteId;
        DeliveryNoteItemId = deliveryNoteItemId;
        AcceptedQuantity = acceptedQuantity;
        RejectedQuantity = rejectedQuantity;
        RejectReason = rejectReason;
    }

    public Guid DeliveryNoteId { get; private set; }
    public Guid DeliveryNoteItemId { get; private set; }
    public decimal AcceptedQuantity { get; private set; }
    public decimal RejectedQuantity { get; private set; }
    public string? RejectReason { get; private set; }
}
