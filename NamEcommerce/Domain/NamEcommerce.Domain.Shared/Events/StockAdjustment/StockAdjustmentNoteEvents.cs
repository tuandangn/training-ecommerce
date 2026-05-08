namespace NamEcommerce.Domain.Shared.Events.StockAdjustment;

/// <summary>
/// Phiếu kiểm kê/điều chỉnh tồn kho vừa được duyệt — handler sẽ áp delta cho từng item.
/// </summary>
public sealed record StockAdjustmentNoteApproved(Guid NoteId, Guid WarehouseId) : DomainEvent;
