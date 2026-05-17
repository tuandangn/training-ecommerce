using NamEcommerce.Domain.Shared.Dtos.DeliveryNotes;
using NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;

namespace NamEcommerce.Domain.Shared.Services.PurchaseOrders;

public interface IDirectShipManager
{
    /// <summary>
    /// Đánh dấu allocation là direct-ship và lưu thông tin giao hàng.
    /// Validation: address bắt buộc không rỗng.
    /// </summary>
    Task MarkAllocationAsDirectShipAsync(
        Guid allocationId,
        string address,
        string? contactName,
        string? contactPhone,
        int priority,
        CancellationToken ct = default);

    /// <summary>
    /// Phân bổ hàng nhận được từ NCC cho các allocation theo thứ tự ưu tiên
    /// (IsDirectShip desc → DirectShipPriority desc → CreatedAt asc).
    /// Trả về kết quả phân bổ để caller tạo stock movement và DN.
    /// </summary>
    Task<DistributeReceivedQuantityResultDto> DistributeReceivedQuantityAsync(
        Guid purchaseOrderItemId,
        decimal receivedQty,
        CancellationToken ct = default);

    /// <summary>
    /// Kiểm tra xem có allocation direct-ship nào còn khả năng nhận hàng (chưa nhận đủ, chưa cancel).
    /// Dùng để fail-fast trước khi phân bổ và lưu DB.
    /// </summary>
    Task<bool> HasReceivableDirectShipAllocationsAsync(
        Guid purchaseOrderItemId,
        CancellationToken ct = default);

    /// <summary>
    /// Khách xác nhận đã nhận hàng — chuyển DN sang Confirmed, raise event để sinh Invoice/Debt.
    /// Stock movement (kho ảo → out) do handler downstream thực hiện.
    /// </summary>
    Task ConfirmDeliveryAsync(
        Guid deliveryNoteId,
        DateTime confirmedAtUtc,
        string? note,
        CancellationToken ct = default);

    /// <summary>
    /// Từ chối giao hàng — DN sang Rejected, raise event để handler chuyển stock kho ảo → kho chính.
    /// </summary>
    Task RejectDeliveryAsync(
        Guid deliveryNoteId,
        string reason,
        CancellationToken ct = default);

    /// <summary>
    /// Cập nhật địa chỉ giao direct-ship sau khi PO đã confirm.
    /// Ghi audit log + raise DirectShipAddressUpdatedEvent.
    /// </summary>
    Task UpdateDirectShipAddressAsync(
        Guid allocationId,
        string newAddress,
        string? newContactName,
        string? newContactPhone,
        Guid editedByUserId,
        string? reason,
        CancellationToken ct = default);

    Task<IList<DeliveryNoteDto>> GetPendingDeliveriesAsync(
        string? keywords,
        DateTime? fromDateUtc,
        DateTime? toDateUtc,
        CancellationToken ct = default);

    Task<IList<DirectShipAllocationStatusDto>> GetDirectShipAllocationsForOrderItemsAsync(
        IReadOnlyList<Guid> orderItemIds,
        CancellationToken ct = default);

    Task<IList<DirectShipAllocationForPoItemDto>> GetDirectShipAllocationsForPoItemsAsync(
        IReadOnlyList<Guid> purchaseOrderItemIds,
        CancellationToken ct = default);
}
