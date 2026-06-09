namespace NamEcommerce.Domain.Shared.Events.Inventory;

/// <summary>
/// Phát ra khi <c>RevalueProductFromAsync</c> supersede các allocation đảo chiều hàng trả
/// (Quantity &lt; 0, OutboundReferenceType = SalesOrder) nhưng không tạo lại chúng sau khi replay.
/// Handler subscribe để tạo notification cảnh báo dữ liệu giá vốn có thể bị lệch.
/// </summary>
public sealed record InventoryCostReturnReversalLost(
    Guid RunId,
    Guid ProductId,
    IReadOnlyCollection<Guid> AffectedDeliveryNoteIds) : DomainEvent;
