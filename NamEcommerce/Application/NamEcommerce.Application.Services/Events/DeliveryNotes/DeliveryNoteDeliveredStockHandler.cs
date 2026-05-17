using MediatR;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Events.DeliveryNotes;
using NamEcommerce.Domain.Shared.Services.DeliveryNotes;
using NamEcommerce.Domain.Shared.Services.Inventory;

namespace NamEcommerce.Application.Services.Events.DeliveryNotes;

/// <summary>
/// Dispatch tồn kho khi phiếu giao hàng chuyển sang trạng thái Delivered.
/// Áp dụng cho MỌI <see cref="NamEcommerce.Domain.Shared.Enums.DeliveryNotes.DeliveryNoteSourceType"/>
/// (ToCustomer và ToVendorReturn) — tách biệt với việc sinh công nợ khách hàng
/// (xem <see cref="DeliveryNoteDeliveredEventHandler"/>).
/// </summary>
public sealed class DeliveryNoteDeliveredStockHandler(
    IDeliveryNoteManager deliveryNoteManager,
    IInventoryStockManager stockManager) : INotificationHandler<DeliveryNoteDelivered>
{
    public async Task Handle(DeliveryNoteDelivered notification, CancellationToken cancellationToken)
    {
        var deliveryNote = await deliveryNoteManager.GetByIdAsync(notification.DeliveryNoteId).ConfigureAwait(false);
        if (deliveryNote is null) return;

        var releaseReservedStock = deliveryNote.OrderId != Guid.Empty
            && deliveryNote.SourceType == DeliveryNoteSourceType.ToCustomer;

        foreach (var item in deliveryNote.Items)
        {
            await stockManager.DispatchStockAsync(
                item.ProductId,
                deliveryNote.WarehouseId,
                item.Quantity,
                deliveryNote.Id,
                Guid.Empty,
                $"Xuất hàng cho phiếu xuất {deliveryNote.Code}",
                releaseReservedStock: releaseReservedStock).ConfigureAwait(false);
        }
    }
}
