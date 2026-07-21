using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;

namespace NamEcommerce.Application.Contracts.PurchaseOrders;

public interface IDirectShipAppService
{
    Task<CommonActionResultDto> MarkAllocationAsDirectShipAsync(MarkAllocationAsDirectShipAppDto dto);

    Task<CommonActionResultDto> UpdateDirectShipAddressAsync(UpdateDirectShipAddressAppDto dto);

    Task<CommonActionResultDto> ConfirmDeliveryAsync(ConfirmDirectShipDeliveryAppDto dto);

    Task<CommonActionResultDto> RejectDeliveryAsync(RejectDirectShipDeliveryAppDto dto);

    Task<IList<PendingDirectShipDeliveryAppDto>> GetPendingDeliveriesAsync(PendingDirectShipFilterAppDto filter);

    Task<IList<DirectShipAllocationStatusAppDto>> GetDirectShipAllocationsForOrderAsync(IReadOnlyList<(Guid orderId, Guid orderItemId)> orderItemIds);

    Task<IList<DirectShipAllocationForPoItemAppDto>> GetDirectShipAllocationsForPoItemsAsync(IReadOnlyList<(Guid purchaseOrderId, Guid purchaseOrderItemId)> purchaseOrderItemIds);
}
