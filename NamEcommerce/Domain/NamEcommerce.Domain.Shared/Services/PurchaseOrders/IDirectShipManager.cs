using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.DeliveryNotes;
using NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;

namespace NamEcommerce.Domain.Shared.Services.PurchaseOrders;

public interface IDirectShipManager
{
    Task MarkAllocationAsDirectShipAsync(Guid allocationId, string address, string? contactName,string? contactPhone, int priority);

    Task<bool> HasReceivableDirectShipAllocationsAsync(Guid purchaseOrderItemId);

    Task<bool> HasReceivedDirectShipAllocationsAsync(Guid orderId);

    Task OnAllocationReceivedAsync(Guid allocationId,
        decimal receivedDelta, Guid sourceGoodsReceiptId,Guid receivedWarehouseId);

    Task ConfirmDeliveryAsync(Guid deliveryNoteId, DateTime confirmedAtUtc,string? note);

    Task RejectDeliveryAsync(Guid deliveryNoteId,Guid returnWarehouseId,string reason);

    Task HandleSoCancelledForReceivedDirectShipAsync(Guid orderId, Guid returnWarehouseId, Guid userId, string? reason);

    Task UpdateDirectShipAddressAsync(Guid allocationId,
        string newAddress, string? newContactName, string? newContactPhone,
        Guid editedByUserId, string? reason);

    Task<IList<DeliveryNoteDto>> GetPendingDeliveriesAsync(string? keywords, DateTime? fromDateUtc, DateTime? toDateUtc);

    Task<IList<DirectShipAllocationStatusDto>> GetDirectShipAllocationsForOrderItemsAsync(IReadOnlyList<SecondaryItemId> orderItemIds);

    Task<IList<DirectShipAllocationForPoItemDto>> GetDirectShipAllocationsForPoItemsAsync(IReadOnlyList<SecondaryItemId> purchaseOrderItemIds);

    Task<Guid> GetTransitWarehouseIdAsync();
}
